using System.Text.Json;
using Dapper;
using Movies.Application.Models;

namespace Movies.Application.Database;

public class DbInitializer
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public DbInitializer(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task InitializeAsync()
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync("""
                                      create table if not exists movies (
                                          id UUID primary key,
                                          slug TEXT not null,
                                          title TEXT not null,
                                          yearofrelease integer not null);
                                      """);
        
        await connection.ExecuteAsync("""
                                     create unique index concurrently if not exists movies_slug_index 
                                     on movies
                                     using btree(slug);
                                     """);
        
        await connection.ExecuteAsync("""
                                      create table if not exists genres (
                                          movieid UUID references movies (id),
                                          name TEXT not null);
                                      """);
        
        await connection.ExecuteAsync("""
                                      create table if not exists ratings (
                                          userid uuid,
                                          movieid UUID references movies (id),
                                          rating integer not null,
                                          primary key (userid, movieid));
                                      """);
        
        // string json = File.ReadAllText("movies.json");
        // var movies = JsonSerializer.Deserialize<List<Movie>>(json)!;
        // foreach (var movie in movies)
        // {
        //     try
        //     {
        //         // Insert into Movies
        //         await connection.ExecuteAsync("""
        //                                           INSERT INTO movies (id, slug, title, yearofrelease)
        //                                           VALUES (@Id, @Slug, @Title, @YearOfRelease)
        //                                           ON CONFLICT (Id) DO NOTHING
        //                                       """, movie);
        //
        //         // Insert Genres
        //         foreach (var genre in movie.Genres)
        //         {
        //             await connection.ExecuteAsync("""
        //                                               INSERT INTO genres (movieid, name)
        //                                               VALUES (@MovieId, @Genre)
        //                                           """, new { MovieId = movie.Id, Genre = genre });
        //         }
        //     }
        //     catch (Exception e)
        //     {
        //         // ignored
        //     }
        // }
    }
}