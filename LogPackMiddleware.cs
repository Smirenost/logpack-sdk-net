using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FeatureNinjas.LogPack.Utilities.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace FeatureNinjas.LogPack
{
  public class LogPackMiddleware
    {
        #region Fields

        private readonly RequestDelegate _next;

        private LogPackOptions _options;

        #endregion

        #region Constructors

        public LogPackMiddleware(RequestDelegate next, LogPackOptions options)
        {
            _next = next;
            _options = options;
        }

        #endregion

        #region Methods

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                Console.WriteLine("Middleware called");

                try
                {
                    await _next(context);

                    if (context.Response?.StatusCode >= 500 && context.Response?.StatusCode < 600)
                    {
                        // handle error codes
                        LogPackTracer.Tracer.Trace(context.TraceIdentifier, "Called middleware returned status code 5xx");

                        await CreateLogPack(context);
                    }
                    else
                    {
                        var createLogPackAfterFilter = false;
                        
                        // handle include filters
                        foreach (var includeFilter in _options.Include)
                        {
                            if (includeFilter.Include(context))
                            {
                                LogPackTracer.Tracer.Trace(context.TraceIdentifier, $"Include filter {nameof(includeFilter)} returned true");

                                createLogPackAfterFilter = true;
                                break;
                            }
                        }
                        
                        // handle exclude filter
                        if (createLogPackAfterFilter == true)
                        {
                            foreach (var excludeFilter in _options.Exclude)
                            {
                                if (excludeFilter.Exclude(context))
                                {
                                    LogPackTracer.Tracer.Trace(context.TraceIdentifier, $"Exclude filter {nameof(excludeFilter)}");
    
                                    createLogPackAfterFilter = false;
                                    break;
                                }
                            }
                        }

                        if (createLogPackAfterFilter)
                            await CreateLogPack(context);
                    }
                }
                catch (System.Exception e)
                {
                    LogPackTracer.Tracer.Trace(context.TraceIdentifier, "Middleware ran into an exception:");
                    LogPackTracer.Tracer.Trace(context.TraceIdentifier, e.Message);
                    if (e.StackTrace != null)
                        LogPackTracer.Tracer.Trace(context.TraceIdentifier, e.StackTrace);

                    await CreateLogPack(context);
                }
                finally
                {
                    LogPackTracer.Tracer.Remove(context.TraceIdentifier);
                }
            }
            catch (System.Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }

        private async Task CreateLogPack(HttpContext context)
        {
            await using var stream = new MemoryStream();
            using var archive = new ZipArchive(stream, ZipArchiveMode.Create, true);
            
            // write .logpack file
            var meta = CreateLogPackFile(archive, context);

            // write logs
            CreateFileForLogs(archive, context);

            // write env
            CreateFileForEnv(archive);

            // write the context
            await CreateFileForHttpContext(archive, context);
            
            // write dependencies
            CreateFileForDependencies(archive, context);
            
            // add response in case enabled by the user
            if (_options.IncludeResponse)
            {
                await CreateFileForResponse(archive, context);
            }

            // add files
            await AddFiles(archive);

            // close the archive
            archive.Dispose();

            // write the zip file
            var rnd = RandomStringGenerator.RandomString(6);
            var sc = context.Response == null ? 0 : context.Response.StatusCode;
            var fileName = $"logpack-{DateTime.Now.ToString("yyyyMMdd-HHmmss")}-{sc}-{rnd}.logpack";
            using var fileStream = new FileStream(fileName, FileMode.Create);
            stream.Seek(0, SeekOrigin.Begin);
            await stream.CopyToAsync(fileStream);
            await stream.DisposeAsync();
            await fileStream.DisposeAsync();

            // upload to FTP
            if (_options != null && _options.Sinks != null)
            {
                foreach (var sink in _options.Sinks)
                {
                    await sink.Send(fileName);
                }
            }

            // delete the local file
            File.Delete(fileName);
            
            // send notifications out
            foreach (var notificationService in _options.NotificationServices)
            {
                await notificationService.Send(fileName, meta);
            }
        }

        private async Task AddFiles(ZipArchive archive)
        {
            if (_options != null && _options.IncludeFiles != null)
            {
                foreach (var includeFile in _options.IncludeFiles)
                {
                    using var fs = File.OpenRead(includeFile);
                    using var fsr = new StreamReader(fs);

                    var ff = archive.CreateEntry(includeFile);
                    using var entryStream = ff.Open();
                    using var streamWriter = new StreamWriter(entryStream);
                    await streamWriter.WriteAsync(fsr.ReadToEnd());

                    streamWriter.Dispose();
                    entryStream.Dispose();
                }
            }
        }

        private string CreateLogPackFile(ZipArchive archive, HttpContext context)
        {
            if (context == null)
                return "context is null";
            
            // setup the stream
            var file = archive.CreateEntry(".logpack");
            using var entryStream = file.Open();
            using var streamWriter = new StreamWriter(entryStream);
            
            // write the file
            var now = DateTime.Now;
            var meta = new StringBuilder();
            meta.AppendLine($"path: {context.Request.Path.ToString()}");
            meta.AppendLine($"date: {now.ToShortDateString()}");
            meta.AppendLine($"time: {now.ToShortTimeString()}");
            meta.AppendLine($"rc: {context.Response.StatusCode}");
            streamWriter.WriteLine(meta);
            
            // close the stream
            streamWriter.Close();
            entryStream.Close();

            return meta.ToString();
        }

        private void CreateFileForLogs(ZipArchive archive, HttpContext context)
        {
            if (context == null)
                return;

            // setup the stream
            var file = archive.CreateEntry("trace.log");
            using var entryStream = file.Open();
            using var streamWriter = new StreamWriter(entryStream);

            // write the logs
            var logger = LogPackTracer.Tracer;
            foreach (var log in logger.Get(context.TraceIdentifier))
            {
                streamWriter.WriteLine(log);
            }

            // remove the logs from memory
            logger.Remove(context.TraceIdentifier);

            // close the stream
            streamWriter.Dispose();
            entryStream.Dispose();
        }

        private void CreateFileForEnv(ZipArchive archive)
        {
            // setup the stream
            var file = archive.CreateEntry("env.log");
            using var entryStream = file.Open();
            using var streamWriter = new StreamWriter(entryStream);

            // write the env variables
            var envVariables = new ProcessStartInfo().EnvironmentVariables;
            foreach (DictionaryEntry env in envVariables)
            {
                streamWriter.WriteLine($"{env.Key}={env.Value}");
            }

            // close the stream
            streamWriter.Dispose();
            entryStream.Dispose();
        }

        private async Task CreateFileForHttpContext(ZipArchive archive, HttpContext context)
        {
            if (context == null)
                return;

            // setup the stream
            var file = archive.CreateEntry("context.log");
            using var entryStream = file.Open();
            using var streamWriter = new StreamWriter(entryStream);

            // write the context
            streamWriter.WriteLine($"{context.Request.Protocol} {context.Request.Path.ToString()} {context.Request.Method}");
            streamWriter.WriteLine($"Host: {context.Request.Host.ToString()}");
            streamWriter.WriteLine($"Request.Query:    {context.Request.QueryString}");
            foreach (var requestHeader in context.Request.Headers)
            {
                streamWriter.WriteLine($"{requestHeader.Key}: {requestHeader.Value}");
            }
            
            // get the request body
            if (_options.IncludeRequestPayload)
            {
                string body = null;
                using (var reader = new StreamReader(context.Request.Body))
                {
                    body = await reader.ReadToEndAsync();
                }
                streamWriter.WriteLine(body);
            }
            
            // close the stream
            streamWriter.Dispose();
            entryStream.Dispose();
        }
        
        private async Task CreateFileForResponse(ZipArchive archive, HttpContext context)
        {
            if (context == null)
                return;

            // setup the stream
            var file = archive.CreateEntry("response");
            using var entryStream = file.Open();
            using var streamWriter = new StreamWriter(entryStream);
            
            // write some response info
            streamWriter.WriteLine($"statusCode: {context.Response.StatusCode}");

            // write the context
            foreach (var requestHeader in context.Response.Headers)
            {
                streamWriter.WriteLine($"{requestHeader.Key}: {requestHeader.Value}");
            }
            
            // get the request body
            if (_options.IncludeResponsePayload)
            {
                string body = null;
                using (var reader = new StreamReader(context.Response.Body))
                {
                    body = await reader.ReadToEndAsync();
                }
                streamWriter.WriteLine(body);
            }
            
            // close the stream
            streamWriter.Dispose();
            entryStream.Dispose();
        }

        private void CreateFileForDependencies(ZipArchive archive, HttpContext context)
        {
            if (context == null)
                return;

            var programAssembly = _options.ProgramType.Assembly;
            if (programAssembly == null)
                return;
            
            // setup the stream
            var file = archive.CreateEntry("deps.log");
            using var entryStream = file.Open();
            using var streamWriter = new StreamWriter(entryStream);

            // write deps to stream
            streamWriter.WriteLine(programAssembly);
            var referencedAssemblies = programAssembly.GetReferencedAssemblies();
            foreach (var referencedAssembly in referencedAssemblies)
            {
                streamWriter.WriteLine("  " + referencedAssembly);
            }

            // close the stream
            streamWriter.Dispose();
            entryStream.Dispose();
        }

        #endregion
    }
}