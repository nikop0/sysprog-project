using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace zadatak2
{
    internal class FileSearcher
    {
        private readonly string _rootPath;

        public FileSearcher(string rootPath)
        {
            _rootPath = rootPath;
        }

        public string Search(string query)
        {
            string[] keywords = query.Split('&', StringSplitOptions.RemoveEmptyEntries);

            var searchResults = new List<(string FileName, Dictionary<string, int> Counts)>();

            try
            {
                string[] files = Directory.GetFiles(_rootPath, "*.txt", SearchOption.TopDirectoryOnly);
                foreach (var filePath in files)
                {
                    string content = File.ReadAllText(filePath);

                    string lowerContent = content.ToLower();

                    string[] wordsInFile = lowerContent.Split(new[] { ' ', '.', ',', '!', '?', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    var counts = new Dictionary<string, int>();
                    bool foundAny = false;

                    foreach (var keyword in keywords)
                    {
                        string lowerKeyword = keyword.ToLower();
                        int currentWordCount = 0;

                        foreach (var word in wordsInFile)
                        {
                            if (word == lowerKeyword)
                            {
                                currentWordCount++;
                            }
                        }

                        counts[keyword] = currentWordCount;
                        if (currentWordCount > 0) foundAny = true;
                    }

                    if (foundAny)
                    {
                        searchResults.Add((Path.GetFileName(filePath), counts));
                    }
                }

                return FormatHtml(searchResults, keywords);
            }
            catch (Exception ex)
            {
                ThreadSafeLogger.Log($"GREŠKA: {ex.Message}");
                return "<html><body><h1>Greška pri pristupu fajlovima.</h1></body></html>";
            }
        }

        private string FormatHtml(List<(string FileName, Dictionary<string, int> Counts)> results, string[] keywords)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<html><head><meta charset='utf-8'><title>Rezultati pretrage</title>");
            sb.Append("<style>table { width: 100%; border-collapse: collapse; } th, td { border: 1px solid black; padding: 8px; text-align: left; } th { background-color: #f2f2f2; }</style>");
            sb.Append("</head><body>");
            sb.Append("<h1>Izveštaj pretrage</h1>");

            if (results.Count == 0)
            {
                sb.Append("<p>Nema rezultata za tražene reči.</p>");
            }
            else
            {
                sb.Append("<table><tr><th>Naziv fajla</th>");
                foreach (var word in keywords)
                {
                    sb.Append($"<th>Pojavljivanja: {word}</th>");
                }
                sb.Append("</tr>");

                foreach (var res in results)
                {
                    sb.Append($"<tr><td>{res.FileName}</td>");
                    foreach (var word in keywords)
                    {
                        sb.Append($"<td>{res.Counts[word]}</td>");
                    }
                    sb.Append("</tr>");
                }
                sb.Append("</table>");
            }

            sb.Append("</body></html>");
            return sb.ToString();
        }
    }
}
