using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Parser;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using WebApplication.Models;

namespace WebApplication.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("[controller]")]
    public class MainController : Controller
    {
        private readonly HttpClient httpClient;
        private IMemoryCache cache;
        private readonly string bodyServerUrl = "https://localhost:44393/";

        private async Task<HttpResponseMessage> GetMessageFromServerAsync(string url)
        {
            return await httpClient.GetAsync(url);
        }

        public MainController(IMemoryCache cache/*, HttpClient httpClient*/)
        {
            this.httpClient = new HttpClient();
            this.cache = cache;
        }

        [Microsoft.AspNetCore.Mvc.HttpPost("CheckEvent")]
        public void CheckEvent(string link)
        {
            Debug.Print($"CheckEvent");
            foreach (var dK in GetCachedKeys())
            {
                if (cache.TryGetValue(dK, out IEnumerable<EventForView> events))
                {
                    var ev = events.Where(e => e.Event.Link == link).LastOrDefault();
                    if (ev != null)
                    {
                        Debug.Print($"Find {link} in {dK}");

                        cache.Remove(dK);

                        var newEv = events.Select(even => { if(even.Event.Link==link) even.IsChecked = true; return even; });

                        cache.Set(dK, newEv, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(30)));
                        
                        return;
                    }
                }
            }
        }

        [Microsoft.AspNetCore.Mvc.HttpPost("GetDistrict")]
        public async Task<ActionResult<string>> PostDistrict(string districtName)
        {
            Debug.Print("Post: " + districtName);
            District content = null;
            HttpResponseMessage responseRes;

            try
            {
                responseRes = await httpClient.GetAsync($"https://localhost:44393/District/ByName?districtName={districtName}");
            }
            catch (HttpRequestException exp)
            {
                Debug.Print(exp.Message);
                responseRes = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            }

            if (responseRes.IsSuccessStatusCode)
            {
                Debug.Print("Success getting message from server");
                var response = await responseRes.Content.ReadAsStringAsync();
                content = JsonConvert.DeserializeObject<District>(response);
            }

            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            };

            return JsonConvert.SerializeObject(content, settings);
        }

        //метод для регулярного обновления новых событий и складирования их в кеш
        //кладем в кеш новый объект для удобного отображения
        [Microsoft.AspNetCore.Mvc.HttpPost("GetNewDistrictEvents")]
        public async Task<ActionResult<string>> PostNewDistrictEvents(string districtName)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            };
            var timeNow = DateTime.Now;
            var time = new DateTime(timeNow.Year,timeNow.Month,timeNow.Day);
            var tItem = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(30));

            if (!cache.TryGetValue(districtName, out IEnumerable<EventForView> events))
            {
                Debug.Print("Post first: " + districtName);

                DateTime? timeN = null;

                var newEvents = await GetViewEventsAsync(districtName, timeN);

                cache.Set(districtName, newEvents, tItem);
            }
            else
            {
                var lastDownloadTime = events?.OrderBy(ev => ev.Event.DateOfDownload).LastOrDefault()?.Event.DateOfDownload ?? time;

                var evev = await GetViewEventsAsync(districtName,lastDownloadTime);
                var result = events;

                Debug.Print($"Post second: {districtName } {lastDownloadTime} {evev.Count()}");

                if (evev != null)
                {
                    result = result?.Concat(evev);
                    cache.Remove(districtName);
                    cache.Set(districtName, result, tItem);
                }
               
            }

            var newEv = ((IEnumerable<EventForView>)cache.Get(districtName))?.Where(ev => !ev.IsChecked && !ev.IsViewed);
            var count = newEv == null ? 0 : newEv.Count();
            return JsonConvert.SerializeObject(count, settings);
        }

        private async Task<IEnumerable<EventForView>> GetViewEventsAsync(string districtName, DateTime? time)
        {
            HttpResponseMessage response;

            if (districtName == "не определенный")
                response = await TryGetAsync(
                $"https://localhost:44393/Events/GetWithoutDistrict?lastEventDownloadTime={time}");
            else
                response = await TryGetAsync(
                $"https://localhost:44393/District/Events?districtName={districtName}&lastEventDownloadTime={time}");

            var newEvents = (await TryGetContent<IEnumerable<Event>>(response))?
                .Select(ev => new EventForView() { Event = ev });

            return newEvents;
        }

        //метод для получения событий для отображения из кеша
        //у событий из кеша меняется флаг
        //
        [Microsoft.AspNetCore.Mvc.HttpPost("GetDistrictEvents")]
        public ActionResult<string> PostDistrictEvents(string districtName)
        {
            Debug.Print("Post: " + districtName);

            IEnumerable<EventForView> events;
            if (cache.TryGetValue(districtName, out events))
            {
                if (events != null && events.Where(ev => !ev.IsChecked).Any())
                {

                    cache.Remove(districtName);
                    var viewedEvents = events.Select(ev => { ev.IsViewed = true; return ev; });
                    cache.Set(districtName, viewedEvents,
                        new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(30)));
                }
            }

            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            };

            return JsonConvert.SerializeObject(events, settings);
        }


        [Microsoft.AspNetCore.Mvc.HttpGet("GetMessageFromServer")]
        public async Task<IActionResult> GetMessageFromServer()
        {
            Debug.Print("GetMessageFromServer without Parameters");
            IEnumerable<Event> content = new List<Event>();
            var res = await httpClient.GetAsync($"https://localhost:44393/Events/All");
            if (res.IsSuccessStatusCode)
            {
                Debug.Print("Success getting message from server");
                var response = await res.Content.ReadAsStringAsync();
                content = JsonConvert.DeserializeObject<IEnumerable<Event>>(response);
            }
            return View("/Views/My/Index.cshtml", content);
        }

        [Microsoft.AspNetCore.Mvc.HttpGet("ByTime")]
        public async Task<IActionResult> GetMessageFromServer(int timeInMinutes)
        {
            Debug.Print(timeInMinutes.ToString());
            var content = "Default";
            var res = await httpClient.GetAsync($"https://localhost:44393/Events/ByTime?timeInMinutes={timeInMinutes}");
            if (res.IsSuccessStatusCode)
                content = await res.Content.ReadAsStringAsync();
            return View("/Views/My/Index.cshtml", content);
        }

        private async Task<HttpResponseMessage> TryGetAsync(string address)
        {
            HttpResponseMessage responseRes;
            try
            {
                responseRes = await httpClient.GetAsync(address);
            }
            catch (HttpRequestException exp)
            {
                Debug.Print(exp.Message);
                responseRes = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            }

            return responseRes;
        }

        private async Task<T> TryGetContent<T>(HttpResponseMessage responseMessage) where T : class
        {
            T result = null;
            if (responseMessage.IsSuccessStatusCode)
            {
                var response = await responseMessage.Content.ReadAsStringAsync();
                result = JsonConvert.DeserializeObject<T>(response);
            }
            return result;
        }

        private List<string> GetCachedKeys()
        {
            var field = typeof(MemoryCache).GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance);
            var collection = field.GetValue(cache) as ICollection;
            var items = new List<string>();
            if (collection != null)
                foreach (var item in collection)
                {
                    var methodInfo = item.GetType().GetProperty("Key");
                    var val = methodInfo.GetValue(item);
                    items.Add(val.ToString());
                }
            return items;
        }
    }
}
