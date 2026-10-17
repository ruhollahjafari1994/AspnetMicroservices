using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AspnetRunBasics
{
    /// <summary>
    /// Defines the ConfirmationModel type.
    /// </summary>
    public class ConfirmationModel : PageModel
    {
        /// <summary>
        /// Gets or sets the Message value.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Describes the OnGetContact operation.
        /// </summary>
        public void OnGetContact()
        {
            Message = "Your email was sent.";
        }

        /// <summary>
        /// Describes the OnGetOrderSubmitted operation.
        /// </summary>
        public void OnGetOrderSubmitted()
        {
            Message = "Your order submitted successfully.";
        }
    }
}
