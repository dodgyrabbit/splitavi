using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Globalization;
using System.IO;
using System.Text;

namespace splitavi
{
    class Program
    {
        static string TimeSpanFormat = @"hh\:mm\:ss\.fff"; 
            
        static void Main(string[] args)
        {
            var rootCommand = new RootCommand()
            {
                new Option<string>(
                    "--timezone",
                    getDefaultValue: () => "Africa/Johannesburg",
                    "The time zone for the source video."
                    ),
                new Argument<FileInfo>("input")
            };
            rootCommand.Description = "Split AVI files based on their time codes. Needs dvanalyzer and ffmpeg on the path.";

            List<Recording> recordings = new List<Recording>();
            rootCommand.Handler = CommandHandler.Create<FileInfo, string>((input, timezone) =>
            {
                if (input.Exists)
                {
                    Console.WriteLine("Exists");
                }

                var dvanalyzer = System.Diagnostics.Process.Start("dvanalyzer", input.FullName);
                dvanalyzer.StartInfo.UseShellExecute = false;
                dvanalyzer.StartInfo.RedirectStandardOutput = true;
                dvanalyzer.Start();
                var output = dvanalyzer.StandardOutput.ReadToEnd();
                dvanalyzer.WaitForExit();

                Console.WriteLine(output);

                StringReader stringReader = new StringReader(output);
                string originalLine;
                Recording recording = null;
                
                while ((originalLine = stringReader.ReadLine()) != null)
                {
                    if (string.IsNullOrEmpty(originalLine))
                    {
                        continue;
                    }
                    string commaSeparatedLine = System.Text.RegularExpressions.Regex.Replace(originalLine,@"\s+",",");
                    var columns = commaSeparatedLine.Split(",");
                    
                    if (recording != null)
                    {
                        recording.EndOffset = TimeSpan.ParseExact(columns[2], TimeSpanFormat, CultureInfo.CurrentCulture);
                        recordings.Add(recording);
                    }

                    recording = new Recording();
                    recording.Frame = int.Parse(columns[1]);
                    recording.StartOffset = TimeSpan.ParseExact(columns[2], TimeSpanFormat, CultureInfo.CurrentCulture);
                    recording.RecordingDateTime = DateTime.ParseExact(columns[4], @"yyyy-MM-dd", CultureInfo.CurrentCulture);
                    recording.RecordingDateTime = recording.RecordingDateTime.Add(TimeSpan.ParseExact(columns[5],  @"hh\:mm\:ss", CultureInfo.CurrentCulture));
                    recording.RecordingDateTime = TimeZoneInfo.ConvertTimeToUtc(recording.RecordingDateTime, TimeZoneInfo.FindSystemTimeZoneById(timezone));
                }
                
                Console.WriteLine($"Total recordings: {recordings.Count}");
                
                var baseFileName = Path.GetFileNameWithoutExtension(input.Name);
                int fileNumber = 0;
                foreach (Recording item in recordings)
                {
                    string arguments = GetFFMpegArguments(item, input.FullName, $"{fileNumber:0000}-{baseFileName}-{item.RecordingDateTime.ToLocalTime().ToString("yyyy-MM-ddThh.mm.ss")}.mp4");

                    Console.WriteLine(arguments);

                    using (var ffmpeg = new System.Diagnostics.Process())
                    {
                        ffmpeg.StartInfo.FileName = "ffmpeg";
                        ffmpeg.StartInfo.Arguments = arguments;
                        ffmpeg.StartInfo.UseShellExecute = false;
                        ffmpeg.StartInfo.RedirectStandardOutput = true;
                        ffmpeg.Start();
                        output = ffmpeg.StandardOutput.ReadToEnd();
                        ffmpeg.WaitForExit();
                    }

                    Console.WriteLine(output);
                    fileNumber++;
                }
            });

            rootCommand.Invoke(args);
        }

        static string GetFFMpegArguments(Recording recording, string inputFile, string outputFile)
        {
            StringBuilder stringBuilder = new StringBuilder();
            
            // Start from this point
            stringBuilder.Append("-ss ");
            stringBuilder.Append(recording.StartOffset.ToString("hh\\:mm\\:ss\\.fff"));
            
            // Duration
            stringBuilder.Append(" -t ");
            stringBuilder.Append(recording.EndOffset.Subtract(recording.StartOffset).ToString("hh\\:mm\\:ss\\.fff"));

            // Input file
            stringBuilder.Append(" -i ");
            stringBuilder.Append(inputFile);

            // Recording creation time.
            stringBuilder.Append(" -metadata ");
            stringBuilder.Append($"creation_time=\"{recording.RecordingDateTime.ToLocalTime():yyyy-MM-dd HH:mm:ssZ}\"");

            // Deinterlace, scale to 720, pad to get to 1280x720
            stringBuilder.Append(" -vf \"pp=ci|a,scale=-1:720,hqdn3d,pad=1280:720:190:0\"");
            
            // Encoding
            stringBuilder.Append(" -c:v libx264 -preset fast -crf 21 ");

            // Final output file name
            stringBuilder.Append(outputFile);
            return stringBuilder.ToString();
        }
    }
}
