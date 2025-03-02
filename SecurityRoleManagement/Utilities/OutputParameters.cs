using System.Collections.Generic;

namespace SecurityRoleManagement.Utilities
{
    public class OutputParameters
    {
        public bool WasSuccessful { get; set; }
        public string ErrorMessage { get; set; }

        public OutputParameters(bool wasSuccessful, string errorMessage)
        {
            WasSuccessful = wasSuccessful;
            ErrorMessage = errorMessage;
        }

        public Dictionary<string, object> ToDictionary(string successKey, string errorKey)
        {
            return new Dictionary<string, object>
            {
                { successKey, WasSuccessful },
                { errorKey, ErrorMessage },
            };
        }
    }
}
