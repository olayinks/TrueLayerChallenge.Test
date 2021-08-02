using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TrueLayerChallenge.Controllers;
using TrueLayerChallenge.Core.Interfaces;
using TrueLayerChallenge.Core.Services;
using TrueLayerChallenge.Models;
using Xunit;

namespace TrueLayerChallenge.Test
{
    public class TrueLayerAPITest
    {
        private Mock<HttpMessageHandler> mockHttpHandler;
        private PokemonController _controller;
        private readonly ILogger<PokemonController> _logger;
        private readonly IConfiguration _config;
        private readonly Dictionary<string,string> configs = new Dictionary<string, string> {
            {"PokemonURL", "https://pokeapi.co/api/v2/pokemon-species/"},
            {"translationURL", "https://api.funtranslations.com/translate/"} 
        };
        private IResponse _response;
        public  TrueLayerAPITest()
        {
            _logger = Mock.Of<ILogger<PokemonController>>();
            _config = new ConfigurationBuilder().AddInMemoryCollection(configs).Build();
            mockHttpHandler = new Mock<HttpMessageHandler>();
        }
        [Theory]
        [MemberData(nameof(PokemonTestData))]
        public async Task ReadPokemonAPI_OkResponseAsync(PokemonDTO pokemonDTO)
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(pokemonDTO)),
            };
            mockHttpHandler
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(response);
            var httpClient = new HttpClient(mockHttpHandler.Object);
            _response = new Response(httpClient);
            _controller = new PokemonController(_logger,_response, _config);
            var result = await _controller.Get("mewtwo");
            var okResult = result as ObjectResult;

            // assert
            Assert.NotNull(okResult);
            Assert.True(okResult is OkObjectResult);
            Assert.IsType<Pokemon>(okResult.Value);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
                       
        }
        public static IEnumerable<object[]> PokemonTestData()
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pokemon.json");
            var json = File.ReadAllText(filePath);
            var pokemons = JsonConvert.DeserializeObject<IEnumerable<PokemonDTO>>(json);
            foreach (var pokemon in pokemons ?? Enumerable.Empty<PokemonDTO>())
            {
                yield return new[] { pokemon };
            }
            
        }
    }
}
