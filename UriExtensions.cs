using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace CoPilot.Speedway
{
    public class UriExtensions : UriMapperBase
    {
        private string tempUri;

        public override Uri MapUri(Uri uri)
        {
            //uri decode
            tempUri = HttpUtility.UrlDecode(uri.ToString());

            // URI association launch for contoso.
            if (tempUri.Contains("copilot-speedway:run"))
            {
                // Map the show products request to SpeedWay.xaml
                return new Uri("/Speedway/View/SpeedWay.xaml", UriKind.Relative);
            }

            // Otherwise perform normal launch.
            return uri;
        }
    }
}
