using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace GannyBot.RESTAPI
{
    internal class GannyAPI
    {
        HttpClient client = new HttpClient();

        //string API_ENDPOINT = "51.81.155.12?module=contract&action=getabi&address=" + address + "&apikey=" + BSC_API_KEY;

        //HttpResponseMessage response = await client.GetAsync(API_ENDPOINT);
        //string contentString = await response.Content.ReadAsStringAsync();
        //dynamic parsedJson = JsonConvert.DeserializeObject(contentString);
        //string parsedString = parsedJson.ToString();
        //return parsedJson;
    }
}
