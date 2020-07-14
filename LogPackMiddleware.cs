using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using FeatureNinjas.LogPack.Utilities.Helpers;
using Microsoft.AspNetCore.Http;

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
                        // handle include filters
                        foreach (var includeFilter in _options.Include)
                        {
                            if (includeFilter.Include(context))
                            {
                                LogPackTracer.Tracer.Trace(context.TraceIdentifier, $"Include filter {nameof(includeFilter)} returned true");
                            
                                await CreateLogPack(context);
                                break;
                            }
                        }
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

            // write logs
            CreateFileForLogs(archive, context);

            // write env
            CreateFileForEnv(archive);

            // write the context
            CreateFileForHttpContext(archive, context);

            // add files
            await AddFiles(archive);

            // close the archive
            archive.Dispose();

            // write the zip file
            var rnd = RandomStringGenerator.RandomString(6);
            var fileName = $"logpack-{DateTime.Now.ToString("yyyyMMdd-HHmmss")}-{rnd}.zip";
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

        private void CreateFileForHttpContext(ZipArchive archive, HttpContext context)
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
            var body = new StreamReader(context.Request.Body).ReadToEnd();
            streamWriter.WriteLine(body);

            // close the stream
            streamWriter.Dispose();
            entryStream.Dispose();
        }

        #endregion
    }
}