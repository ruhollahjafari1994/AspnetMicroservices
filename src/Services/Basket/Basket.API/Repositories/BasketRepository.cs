using Basket.API.Entities;
using Basket.API.Repositories.Interfaces;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Basket.API.Repositories
{
    /// <summary>
    /// Stores active baskets and checkout reservations in Redis using atomic scripts.
    /// </summary>
    public sealed class BasketRepository : IBasketRepository
    {
        private const string UpdateBasketScript = @"
local current = tonumber(redis.call('GET', KEYS[2]) or '0')
if current ~= tonumber(ARGV[1]) then return 0 end
redis.call('SET', KEYS[1], ARGV[2])
redis.call('SET', KEYS[2], ARGV[3])
return 1";

        private const string ReserveBasketScript = @"
local previous = redis.call('HGET', KEYS[3], 'Status')
if previous then return { previous, redis.call('HGET', KEYS[3], 'Snapshot') or '' } end
if redis.call('GET', KEYS[2]) then return { 'Busy', '' } end
local raw = redis.call('GET', KEYS[1])
if not raw then return { 'Empty', '' } end
local cart = cjson.decode(raw)
local items = cart.Items or cart.items
if not items or #items == 0 then return { 'Empty', '' } end
local version = tonumber(redis.call('GET', KEYS[4]) or '0') + 1
redis.call('HSET', KEYS[3], 'Status', 'Reserved', 'Snapshot', raw, 'UserName', ARGV[1])
redis.call('SET', KEYS[2], ARGV[2])
local emptyCart = '{""UserName"":' .. cjson.encode(ARGV[1]) .. ',""Items"":[],""Version"":' .. version .. '}'
redis.call('SET', KEYS[1], emptyCart)
redis.call('SET', KEYS[4], version)
return { 'Reserved', raw }";

        private const string CompleteCheckoutScript = @"
local status = redis.call('HGET', KEYS[2], 'Status')
if status == 'Committed' then return 1 end
if status ~= 'Reserved' then return 0 end
redis.call('HSET', KEYS[2], 'Status', 'Committed')
if redis.call('GET', KEYS[1]) == ARGV[1] then redis.call('DEL', KEYS[1]) end
return 1";

        private const string RestoreCheckoutScript = @"
local status = redis.call('HGET', KEYS[3], 'Status')
if status == 'Restored' then return 2 end
if status ~= 'Reserved' and status ~= 'Committed' then return 0 end
local version = tonumber(redis.call('GET', KEYS[2]) or '0')
if version ~= tonumber(ARGV[1]) then return -1 end
redis.call('SET', KEYS[1], ARGV[2])
redis.call('SET', KEYS[2], ARGV[3])
redis.call('HSET', KEYS[3], 'Status', 'Restored')
if redis.call('GET', KEYS[4]) == ARGV[4] then redis.call('DEL', KEYS[4]) end
return 1";

        private const string DeleteBasketScript = @"
redis.call('DEL', KEYS[1])
return redis.call('INCR', KEYS[2])";

        private readonly IDatabase _redis;
        private readonly string _keyPrefix;

        /// <summary>
        /// Creates a Redis-backed basket repository.
        /// </summary>
        public BasketRepository(IConnectionMultiplexer redis, string keyPrefix = "basket")
        {
            _redis = (redis ?? throw new ArgumentNullException(nameof(redis))).GetDatabase();
            _keyPrefix = string.IsNullOrWhiteSpace(keyPrefix) ? "basket" : keyPrefix.TrimEnd(':');
        }

        /// <inheritdoc />
        public async Task<ShoppingCart> GetBasket(string userName)
        {
            var keys = UserKeys(userName);
            var values = await _redis.StringGetAsync(new RedisKey[] { keys.Basket, keys.Version });
            var basket = values[0].HasValue
                ? JsonConvert.DeserializeObject<ShoppingCart>(values[0])
                : new ShoppingCart(userName);
            basket ??= new ShoppingCart(userName);
            basket.UserName = userName;
            basket.Version = ParseVersion(values[1], basket.Version);
            return basket;
        }

        /// <inheritdoc />
        public async Task<bool> UpdateBasket(ShoppingCart basket, long expectedVersion)
        {
            var keys = UserKeys(basket.UserName);
            var nextVersion = expectedVersion + 1;
            basket.Version = nextVersion;
            var result = await _redis.ScriptEvaluateAsync(
                UpdateBasketScript,
                new RedisKey[] { keys.Basket, keys.Version },
                new RedisValue[]
                {
                    expectedVersion,
                    JsonConvert.SerializeObject(basket),
                    nextVersion
                });

            return (int)result == 1;
        }

        /// <inheritdoc />
        public async Task DeleteBasket(string userName)
        {
            var keys = UserKeys(userName);
            await _redis.ScriptEvaluateAsync(DeleteBasketScript,
                new RedisKey[] { keys.Basket, keys.Version }, Array.Empty<RedisValue>());
        }

        /// <inheritdoc />
        public async Task<BasketCheckoutReservation> ReserveBasket(string userName, Guid checkoutId)
        {
            var keys = UserKeys(userName);
            var record = CheckoutKey(userName, checkoutId);
            var result = (RedisResult[])await _redis.ScriptEvaluateAsync(
                ReserveBasketScript,
                new RedisKey[] { keys.Basket, keys.Lock, record, keys.Version },
                new RedisValue[] { userName, checkoutId.ToString("N") });
            var state = Enum.Parse<BasketCheckoutReservationState>(result[0].ToString(), true);
            var snapshot = string.IsNullOrEmpty(result[1].ToString())
                ? null
                : JsonConvert.DeserializeObject<ShoppingCart>(result[1].ToString());

            if (snapshot != null)
            {
                snapshot.UserName = userName;
            }

            return new BasketCheckoutReservation { State = state, Snapshot = snapshot };
        }

        /// <inheritdoc />
        public async Task<BasketCheckoutReservation> GetCheckoutReservation(string userName, Guid checkoutId)
        {
            var entries = await _redis.HashGetAllAsync(CheckoutKey(userName, checkoutId));
            if (entries.Length == 0)
            {
                return null;
            }

            var state = Enum.Parse<BasketCheckoutReservationState>(
                entries.Single(entry => entry.Name == "Status").Value.ToString(), true);
            var snapshot = JsonConvert.DeserializeObject<ShoppingCart>(
                entries.Single(entry => entry.Name == "Snapshot").Value.ToString());
            if (snapshot != null)
            {
                snapshot.UserName = userName;
            }

            return new BasketCheckoutReservation { State = state, Snapshot = snapshot };
        }

        /// <inheritdoc />
        public async Task<bool> CompleteCheckout(string userName, Guid checkoutId)
        {
            var keys = UserKeys(userName);
            var result = await _redis.ScriptEvaluateAsync(
                CompleteCheckoutScript,
                new RedisKey[] { keys.Lock, CheckoutKey(userName, checkoutId) },
                new RedisValue[] { checkoutId.ToString("N") });
            return (int)result == 1;
        }

        /// <inheritdoc />
        public async Task<bool> RestoreCheckout(string userName, Guid checkoutId)
        {
            var keys = UserKeys(userName);
            var record = CheckoutKey(userName, checkoutId);
            var redisRecord = await _redis.HashGetAllAsync(record);
            if (redisRecord.Length == 0)
            {
                return false;
            }

            var snapshotJson = redisRecord.SingleOrDefault(entry => entry.Name == "Snapshot").Value;
            var snapshot = JsonConvert.DeserializeObject<ShoppingCart>(snapshotJson);
            if (snapshot?.Items == null || snapshot.Items.Count == 0)
            {
                return false;
            }

            for (var attempt = 0; attempt < 20; attempt++)
            {
                var currentValues = await _redis.StringGetAsync(new RedisKey[] { keys.Basket, keys.Version });
                var version = ParseVersion(currentValues[1], 0);
                var current = currentValues[0].HasValue
                    ? JsonConvert.DeserializeObject<ShoppingCart>(currentValues[0])
                    : new ShoppingCart(userName);
                current ??= new ShoppingCart(userName);
                MergeItems(current, snapshot);
                current.Version = version + 1;

                var result = await _redis.ScriptEvaluateAsync(
                    RestoreCheckoutScript,
                    new RedisKey[] { keys.Basket, keys.Version, record, keys.Lock },
                    new RedisValue[]
                    {
                        version,
                        JsonConvert.SerializeObject(current),
                        version + 1,
                        checkoutId.ToString("N")
                    });
                var outcome = (int)result;
                if (outcome == 1 || outcome == 2)
                {
                    return true;
                }

                if (outcome == 0)
                {
                    return false;
                }
            }

            throw new InvalidOperationException("Could not restore the basket after repeated concurrent updates.");
        }

        private (RedisKey Basket, RedisKey Version, RedisKey Lock) UserKeys(string userName)
        {
            var owner = Uri.EscapeDataString(userName.Trim().ToLowerInvariant());
            return (userName, $"{_keyPrefix}:version:{owner}", $"{_keyPrefix}:checkout-lock:{owner}");
        }

        private RedisKey CheckoutKey(string userName, Guid checkoutId)
        {
            var owner = Uri.EscapeDataString(userName.Trim().ToLowerInvariant());
            return $"{_keyPrefix}:checkout:{owner}:{checkoutId:N}";
        }

        private static long ParseVersion(RedisValue value, long fallback) =>
            long.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var version)
                ? version
                : fallback;

        private static void MergeItems(ShoppingCart target, ShoppingCart source)
        {
            target.UserName = source.UserName;
            target.Items ??= new System.Collections.Generic.List<ShoppingCartItem>();
            foreach (var restored in source.Items)
            {
                var existing = target.Items.FirstOrDefault(item =>
                    string.Equals(item.ProductId, restored.ProductId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(item.Color ?? string.Empty, restored.Color ?? string.Empty, StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                {
                    target.Items.Add(new ShoppingCartItem
                    {
                        ProductId = restored.ProductId,
                        ProductName = restored.ProductName,
                        Quantity = restored.Quantity,
                        Color = restored.Color,
                        Price = restored.Price
                    });
                }
                else
                {
                    existing.Quantity = checked(existing.Quantity + restored.Quantity);
                }
            }
        }
    }
}
