using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace DigiMenuAPI.Controllers
{
    [Route("api/configuration")]
    [ApiController]
    public class ConfigurationController : Controller
    {
        private readonly IOutputCacheStore outputCacheStore;
        private const string productCacheTag = "Products";
        public ConfigurationController(IOutputCacheStore outputCacheStore)
        {
            this.outputCacheStore = outputCacheStore;
            this.outputCacheStore = outputCacheStore;
        }

        // GET: api/<Product>
        [HttpGet]
        [OutputCache(Tags = [productCacheTag])]
        public IEnumerable<string> GetAll()
        {
            return new string[] { "value1", "value2" };
        }

        [HttpGet("{id}")]
        public string GetOne(int id)
        {
            return "value";
        }

        // POST api/<Product>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<Product>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<Product>/5
        [HttpDelete("{id}")]
        public async Task Delete(int id)
        {

            await outputCacheStore.EvictByTagAsync(productCacheTag, default);
        }

    }
}
