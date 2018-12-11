using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace DataProtectionSample.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        // GET api/values
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new[]{
                new
                {
                    id = 1,
                    val = "value1"
                },
                new
                {
                    id =2,
                    val ="value2"
                } });
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            return Ok(new { getId = id, value = "value" });
        }

        [HttpGet("test")]
        public IActionResult AnotherGetTest()
        {
            return new JsonResult(new[]{
                new
                {
                    id = 1,
                    val = "value1"
                },
                new
                {
                    id =2,
                    val ="value2"
                } });
        }

        // POST api/values
        [HttpPost]
        public IActionResult Post([FromBody] FooModel model)
        {
            return Ok(new { postId = model.Id, value = model.Value });
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody] FooModel model)
        {
            return Ok(new { routeId = id, putId = model.Id, value = model.Value });
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            return Ok(new { delId = id });
        }
    }

    public class FooModel
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
