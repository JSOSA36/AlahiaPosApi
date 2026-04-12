using Twilio;
using Twilio.Types;
using Twilio;
using Twilio.Types;
using Twilio.Rest.Api.V2010.Account;

namespace AlahiaPosApi
{
    public class TwilioService
    {
        private readonly string _accountSid;
        private readonly string _authToken;
        private readonly string _fromWhatsApp;

        public TwilioService(IConfiguration config)
        {
            _accountSid = config["Twilio:AccountSid"];
            _authToken = config["Twilio:AuthToken"];
            _fromWhatsApp = config["Twilio:FromWhatsApp"];
        }

        public void EnviarWhatsApp(string numeroDestino, string mensaje)
        {
            TwilioClient.Init(_accountSid, _authToken);

            var message = MessageResource.Create(
                body: mensaje,
                from: new PhoneNumber(_fromWhatsApp),
                to: new PhoneNumber($"whatsapp:{numeroDestino}")
            );
        }
    }
}
