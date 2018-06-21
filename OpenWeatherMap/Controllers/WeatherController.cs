using Newtonsoft.Json;
using OpenWeatherMap.Helpers;
using OpenWeatherMap.Models;
using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Mvc;

namespace OpenWeatherMap.Controllers
{
    public class WeatherController : Controller
    {
        public ActionResult Index(string search, string unit)
        {
            if (search == null)
            {
                search = "singapore,sg"; //>> Default city
            }

            if (unit == null)
            {
                unit = "metric"; //>> Default unit
            }

            WeatherModel weather = GetWeather(search, unit);
            return View(weather);
        }

        public WeatherModel GetWeather(string id, string unit)
        {
            WeatherModel weather = new WeatherModel();

            try
            {
                if (id != null)
                {
                    string apiKey = ConfigurationManager.AppSettings["apiKey"];

                    //string latitude = Request["latitude"].ToString();
                    //string longitude = Request["longitude"].ToString();

                    HttpWebRequest apiRequest = null;
                    apiRequest = WebRequest.Create(string.Format(@"http://api.openweathermap.org/data/2.5/forecast?q={0}&appid={1}&units={2}", id, apiKey, unit)) as HttpWebRequest;

                    string apiResponse = "";
                    using (HttpWebResponse response = apiRequest.GetResponse() as HttpWebResponse)
                    {
                        StreamReader reader = new StreamReader(response.GetResponseStream());
                        apiResponse = reader.ReadToEnd();
                    }

                    ResponseWeather res = JsonConvert.DeserializeObject<ResponseWeather>(apiResponse);

                    StringBuilder sb = new StringBuilder();

                    TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

                    DateTime mainDt = res.list[0].dt; //>> The main display weather datetime

                    //>> Main top, today's weather display

                    sb.AppendFormat(@"<div class=""location"">{0}, {1}</div>", res.city.name, res.city.country);
                    sb.AppendFormat(@"<div class=""date"">{0}, {1} {2}{3}</div>", res.list[0].dt.DayOfWeek, res.list[0].dt.ToString("MMMM"), res.list[0].dt.Day, GetDaySuffix(res.list[0].dt.Day));
                    sb.AppendFormat(@"<div class=""desc"">{0}</div>", textInfo.ToTitleCase(res.list[0].weather[0].description));
                    sb.AppendFormat(@"<div class=""current"">"); //>> Start: "current" class
                    sb.AppendFormat(@"<div class=""visual"">"); //>> Start: "visual" class
                    sb.AppendFormat(@"<div class=""icon""><img src=""http://openweathermap.org/img/w/{0}.png""/></div>", res.list[0].weather[0].icon);
                    sb.AppendFormat(@"<div class=""temp"">{0}</div>", res.list[0].main.temp);
                    sb.AppendFormat(@"<div class=""scale""><a class=""{0}"" href=""{1}"">°C</a> | <a class=""{2}"" href=""{3}"">°F</a></div>"
                                    , unit == "metric" ? "unit active" : "unit"
                                    , unit == "metric" ? "javascript:void(0);" : "/Weather?search=" + id + "&unit=metric"
                                    , unit == "imperial" ? "unit active" : "unit"
                                    , unit == "imperial" ? "javascript:void(0);" : "/Weather?search=" + id + "&unit=imperial");

                    sb.AppendFormat(@"</div>"); //>> End: "visual" class
                    sb.AppendFormat(@"<div class=""description"">"); //>> Start: "description" class
                    sb.AppendFormat(@"<div class=""humidity"">{0}</div>", res.list[0].main.humidity);
                    sb.AppendFormat(@"<div class=""wind"">{0} {1}</div>", res.list[0].wind.speed, res.list[0].wind.deg);
                    sb.AppendFormat(@"<div class=""pressure"">{0}</div>", res.list[0].main.pressure);
                    sb.AppendFormat(@"</div>"); //>> End: "description" class
                    sb.AppendFormat(@"</div>"); //>> End: "current" class

                    //>> Bottom 5-day weather display

                    sb.AppendFormat(@"<div class=""seven-day"">");

                    //>> Display 5-day forecast
                    //>> There will be times, when next 5-days forecast will only show 4 days, due to not sufficient forecast data for a particular time range
                    //>> Changing to 16-day forcast api, will resolve the issue (Paid API)
                    for (int d = 1; d <= 5; d++)
                    {
                        DateTime currentDt = mainDt.AddDays(d); //>> Loop and get the next five days by adding days to main weather datetime

                        for (int i = 0; i < res.list.Count; i++)
                        {
                            //>> If res has the matching date and time, display weather info for that day
                            if (currentDt == res.list[i].dt)
                            {
                                sb.AppendFormat(@"<div class=""seven-day-fc"">");
                                sb.AppendFormat(@"<div class=""date"">{0}</div>", res.list[i].dt.DayOfWeek); //>> i-1? The actual data is in the previous index
                                sb.AppendFormat(@"<div class=""icon""><img src=""http://openweathermap.org/img/w/{0}.png""/></div>", res.list[i].weather[0].icon);
                                sb.AppendFormat(@"<div class=""seven-day-temp"">");
                                sb.AppendFormat(@"<div class=""temp-high"">{0}&deg;&nbsp;</div>", res.list[i].main.temp_max);
                                sb.AppendFormat(@"<div class=""temp-low"">{0}&deg;</div>", res.list[i].main.temp_min);
                                sb.AppendFormat(@"</div>"); //>> End: "day-temp" class
                                sb.AppendFormat(@"</div>"); //>> End: "day-fc" class
                            }
                        }
                    }

                    sb.AppendFormat(@"</div>"); //>> End: "day" class

                    weather.apiResponse = sb.ToString();
                }
            }
            catch { }

            return weather;
        }

        /// <summary>
        /// Get suffix by day
        /// </summary>
        /// <param name="day"></param>
        /// <returns></returns>
        string GetDaySuffix(int day)
        {
            switch (day)
            {
                case 1:
                case 21:
                case 31:
                    return "st";
                case 2:
                case 22:
                    return "nd";
                case 3:
                case 23:
                    return "rd";
                default:
                    return "th";
            }
        }

    }
}