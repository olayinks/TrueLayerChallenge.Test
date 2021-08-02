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
        private string translatedText = @"{
    ""success"": {
        ""total"": 1
    },
    ""contents"": {
        ""translated"": ""Mewtwo is a pokémon yond wast did create by genetic manipulation. However,  coequal though the scientific power of humans did create this pokémon’s corse,  they did fail to endow mewtwo with a compassionate heart."",
        ""text"": ""MEWTWO is a POKéMON that was created\nby genetic manipulation.However, even though the scientific power of humans created this POKéMON’s body, they failed to endow MEWTWO with a compassionate heart."",
        ""translation"": ""shakespeare""
    }
}";
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

        [Theory]
        [MemberData(nameof(PokemonTestData))]
        public async Task ReadPokemonAPI_BadRequestResponseAsync(PokemonDTO pokemonDTO)
        {

            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
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
            _controller = new PokemonController(_logger, _response, _config);
            var result = await _controller.Get("mewtwo");
            var notFoundResult = result as ObjectResult;

            // assert
            //Assert.NotNull(notFoundResult);
            //Assert.True(notFoundResult is NotFoundObjectResult);
            Assert.Equal(StatusCodes.Status400BadRequest, notFoundResult.StatusCode);


        }

        [Theory]
        [MemberData(nameof(PokemonTestData))]
        public async Task TranslatePokemonAPI_TranslatedResponseAsync(PokemonDTO pokemonDTO)
        {
            
            //Arrange for Pokemon Fetch
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


            //Act for Pokemon Fetch
            _response = new Response(httpClient);
            var pokemon = await _response.FetchPokemon($"{_config["PokemonURL"]}mewtwo");

            //assert for pokemon fetch
            Assert.NotNull(pokemon);
            Assert.IsType<Pokemon>(pokemon);

            //arrange for translation
            response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(translatedText),
            };
            mockHttpHandler
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(response);
             httpClient = new HttpClient(mockHttpHandler.Object);

            //Act for Translation
            _response = new Response(httpClient);
            var pokemonTranslated = await _response.Translation($"{_config["translationURL"]}",pokemon);

            string res = JsonConvert.DeserializeObject<dynamic>(translatedText).contents.translated;
            // assert for translation
            Assert.NotNull(pokemonTranslated);
            Assert.Contains(res, pokemonTranslated.Description);


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
