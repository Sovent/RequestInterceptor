module Interceptor.App

open FSharp.Control.Tasks
open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Newtonsoft.Json
open Giraffe

// ---------------------------------
// Web app
// ---------------------------------

type Response = {
    Code: int
}

let log : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let response = { Code = 0 }
            let! request = ctx.BindJsonAsync<Object>()
            printfn "-------%O-------" DateTime.Now
            printfn "%s request to route %s" ctx.Request.Method (ctx.Request.Path.ToString())
            printfn "Query string %s" (ctx.Request.QueryString.ToString())
            printfn "Headers"
            for header in ctx.Request.Headers do
                printfn "%s : %s" header.Key (String.Join(",", header.Value))
            printfn "Body: %s" (JsonConvert.SerializeObject(request))
            
            return! json response next ctx
        }       

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (builder : CorsPolicyBuilder) =
    builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader() |> ignore

let configureApp (app : IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IHostingEnvironment>()
    (match env.IsDevelopment() with
    | true  -> app.UseDeveloperExceptionPage()
    | false -> app.UseGiraffeErrorHandler errorHandler)
        .UseHttpsRedirection()
        .UseCors(configureCors)
        .UseStaticFiles()
        .UseGiraffe(log)

let configureServices (services : IServiceCollection) =
    services.AddCors()    |> ignore
    services.AddGiraffe() |> ignore

let configureLogging (builder : ILoggingBuilder) =
    builder.AddFilter(fun l -> l.Equals LogLevel.Error)
           .AddConsole()
           .AddDebug() |> ignore

[<EntryPoint>]
let main _ =
    WebHostBuilder()
        .UseKestrel()
        .UseIISIntegration()
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .ConfigureLogging(configureLogging)
        .Build()
        .Run()
    0