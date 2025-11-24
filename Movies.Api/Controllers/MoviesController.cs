using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Movies.Api.Auth;
using Movies.Api.Mapping;
using Movies.Application.Services;
using Movies.Contracts.Requests;
using Movies.Contracts.Responses;

namespace Movies.Api.Controllers;

[ApiController]
//[ApiVersion(1.0, Deprecated = true)]
[ApiVersion(1.0)]
[ApiVersion(2.0)]
public class MoviesController : ControllerBase
{
    private readonly IMovieService _movieService;
    private readonly IOutputCacheStore _outputCacheStore;

    public MoviesController(IMovieService movieService, IOutputCacheStore outputCacheStore)
    {
        _movieService = movieService;
        _outputCacheStore = outputCacheStore;
    }

    // [Authorize(AuthConstants.TrustedMemberPolicyName)]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    [HttpPost(ApiEndpoints.Movies.Create)]
    [ProducesResponseType(type: typeof(MovieResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(type: typeof(ValidationFailureResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody]CreateMovieRequest request, CancellationToken token)
    {
        var movie = request.MapToMovie();
        await _movieService.CreateAsync(movie, token);
        await _outputCacheStore.EvictByTagAsync("movies", token);
        var response = movie.MapToResponse();
        return CreatedAtAction(nameof(GetV1) ,new {id = movie.Id}, response);
    }
    
    [MapToApiVersion(1.0)]
    [HttpGet(ApiEndpoints.Movies.Get)]
    [OutputCache(PolicyName = "MovieCache")]
    // [ResponseCache(Duration = 30, VaryByHeader = "Accept, Accept-Encoding", Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(type: typeof(MovieResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetV1([FromRoute]string idOrSlug, [FromServices] LinkGenerator linkGenerator, CancellationToken token)
    {
        var userId = HttpContext.GetUserId();
        var movie = Guid.TryParse(idOrSlug, out var id) ? await _movieService.GetByIdAsync(id, userId, token) : await _movieService.GetBySlugAsync(idOrSlug, userId, token);

        if (movie is null)
        {
            return NotFound();
        }
        
        var response = movie.MapToResponse();
        
        var movieObj = new { id = movie.Id };
        // response.Links.Add(new Link
        // {
        //     Href = linkGenerator.GetPathByAction(HttpContext, nameof(GetV1), values: new { idOrSlug = movieObj.id }),
        //     Rel = "self",
        //     Type = "GET"
        // });
        // response.Links.Add(new Link
        // {
        //     Href = linkGenerator.GetPathByAction(HttpContext, nameof(Update), values: new { idOrSlug = movieObj.id }),
        //     Rel = "self",
        //     Type = "PUT"
        // });
        // response.Links.Add(new Link
        // {
        //     Href = linkGenerator.GetPathByAction(HttpContext, nameof(Delete), values: new { idOrSlug = movieObj.id }),
        //     Rel = "self",
        //     Type = "DELETE"
        // });
        
        return Ok(response);
    }
    
    [MapToApiVersion(2.0)]
    [HttpGet(ApiEndpoints.Movies.Get)]
    [OutputCache(PolicyName = "MovieCache")]
    // [ResponseCache(Duration = 30, VaryByHeader = "Accept, Accept-Encoding", Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(type: typeof(MovieResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetV2([FromRoute]string idOrSlug, [FromServices] LinkGenerator linkGenerator, CancellationToken token)
    {
        var userId = HttpContext.GetUserId();
        var movie = Guid.TryParse(idOrSlug, out var id) ? await _movieService.GetByIdAsync(id, userId, token) : await _movieService.GetBySlugAsync(idOrSlug, userId, token);

        if (movie is null)
        {
            return NotFound();
        }
        
        var response = movie.MapToResponse();
        return Ok(response);
    }
    
    [HttpGet(ApiEndpoints.Movies.GetAll)]
    [OutputCache(PolicyName = "MovieCache")]
    // [ResponseCache(Duration = 30, VaryByQueryKeys = ["title", "year", "sortBy", "page", "pageSize"], Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(type: typeof(MoviesResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] GetAllMoviesRequest request, CancellationToken token)
    {
        var userId = HttpContext.GetUserId();
        var options = request.MapToOptions()
            .WithUserId(userId);
        var movies = await _movieService.GetAllAsync(options, token);
        var moviesCount = await _movieService.GetCountAsync(options.Title, options.YearOfRelease, token);
        var response = movies.MapToResponse(request.Page, request.PageSize, moviesCount);
        
        return Ok(response);
    }
    
    [Authorize(AuthConstants.TrustedMemberPolicyName)]
    [HttpPut(ApiEndpoints.Movies.Update)]
    [ProducesResponseType(type: typeof(MovieResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(type: typeof(ValidationFailureResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromRoute]Guid id, 
        [FromBody]UpdateMovieRequest request, CancellationToken token)
    {
        var movie = request.MapToMovie();
        var userId = HttpContext.GetUserId();
        var updatedMovie = await _movieService.UpdateAsync(movie, userId, token);

        if (updatedMovie is null)
        {
            return NotFound();
        }
        await _outputCacheStore.EvictByTagAsync("movies", token);
        var response = movie.MapToResponse();
        return Ok(response);
    }
    
    [Authorize(AuthConstants.AdminUserPolicyName)]
    [HttpDelete(ApiEndpoints.Movies.Delete)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute]Guid id, CancellationToken token)
    {
        var deleted = await _movieService.DeleteAsync(id, token);

        if (!deleted)
        {
            return NotFound();
        }
        await _outputCacheStore.EvictByTagAsync("movies", token);
        return Ok();
    }
}