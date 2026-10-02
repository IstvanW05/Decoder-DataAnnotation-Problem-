using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        string? url = null;

        while (true)
        {
            try
            {
                Console.Write("Enter the URL of the published Google Doc: ");
                url = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(url))
                {
                    Console.WriteLine("URL cannot be empty.");
                    continue;
                }
                await Decode(url!);
                Console.WriteLine();
            }
            catch
            {
                Console.WriteLine("An error occurred while processing the request.");
                Console.WriteLine();
            }
        }
    }

    static async Task Decode(string url)
    {
        //Console.WriteLine("Downloading published Google Doc...");
        string html = await DownloadHtml(url);

        // Console.WriteLine(html); // Check if the HTML is being printed correctly

        //Console.WriteLine("Parsing table(s)...");
        var tables = ExtractTables(html);

        //Console.WriteLine("Printing table contents:\n");

        int tableNumb = 1;
        // Print each table's contents
        //Console.WriteLine($"Table {tableNumb}:");
        //foreach (var table in tables)
        //{
        //    foreach (var row in table)
        //    {
        //        foreach (var cell in row)
        //        {
        //            Console.Write($"{cell} ");
        //        }
        //    }
        //    tableNumb++;
        //}

        Console.WriteLine();

        // Catagorize the table contents
        foreach (var table in tables)
        {
            int? xIndex = null;
            int? yIndex = null;
            int? unicodeIndex = null;

            var firstRow = table[0];
            for (int i = 0; i < firstRow.Count; i++)
            {
                string cell = firstRow[i].ToLower();
                if (cell.Contains('x'))
                    xIndex = i;
                else if (cell.Contains('y'))
                    yIndex = i;
                else
                    unicodeIndex = i;
            }

            if (xIndex.HasValue && yIndex.HasValue && unicodeIndex.HasValue)
            {
                var points = new List<(int x, int y, string unicode)> ();

                // Console.WriteLine($"Table contains x-coords at index {xIndex}, y-coords at index {yIndex}, and Unicode value at index {unicodeIndex}.");

                for (int i = 1; i < table.Count; i++) // skip header row
                {
                    var row = table[i];
                    if (row.Count > Math.Max(xIndex.Value, yIndex.Value))
                    {
                        string xCoord = row[xIndex.Value];
                        string yCoord = row[yIndex.Value];
                        string unicodeValue = row[unicodeIndex.Value];

                        int x  = int.Parse(row[xIndex.Value]);
                        int y = int.Parse(row[yIndex.Value]);

                        points.Add((x, y, unicodeValue)); // Convert to int and store in points list

                        //Console.WriteLine($"x: {xCoord}, y: {yCoord}, Unicode: {unicodeValue}");
                    }
                }

                // Define print size
                int maxX = points.Max(p => p.x);
                int maxY = points.Max(p => p.y);

                //Console.WriteLine($"Grid is {maxX} x {maxY}");
                //Console.WriteLine("Printing table as a grid:\n");

                for (int y = maxY; y >= 0; y--)
                {
                    for (int x = 0; x <= maxX; x++)
                    {
                        var point = points.Find(p => p.x == x && p.y == y);

                        if (point != default)
                            Console.Write(point.unicode);
                        else
                            Console.Write(" "); // Empty space for missing points
                    }
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine($"Failed to categorize table: {tableNumb}'s contents.");
            }
        }
    }
    static async Task<string> DownloadHtml(string url)
    {
        var client = new HttpClient();
        return await client.GetStringAsync(url);
    }

    static List<List<List<string>>> ExtractTables(string html) // Table > Row > Cell
    {
        var table = new List<List<List<string>>>();
        int tableStart = html.IndexOf("<table", StringComparison.OrdinalIgnoreCase); // Starting Point

        while (tableStart != -1)
        {
            int tableEnd = html.IndexOf("</table>", tableStart, StringComparison.OrdinalIgnoreCase); // Checkpoint/Endpoint
            if (tableEnd == -1) break;

            string tableHtml = html.Substring(tableStart, tableEnd - tableStart); // extract

            var rows = new List<List<string>>(); // Rows in the current table
            int rowStart = tableHtml.IndexOf("<tr", StringComparison.OrdinalIgnoreCase); // Starting Point
            while (rowStart != -1)
            {
                int rowEnd = tableHtml.IndexOf("</tr>", rowStart, StringComparison.OrdinalIgnoreCase); // Checkpoint/Endpoint
                if (rowEnd == -1) break;

                string rowHtml = tableHtml.Substring(rowStart, rowEnd - rowStart); // Extract

                var cells = new List<string>(); // Cells in the current row
                int cellStart = rowHtml.IndexOf("<td", StringComparison.OrdinalIgnoreCase); // Starting Point
                while (cellStart != -1)
                {
                    int cellEnd = rowHtml.IndexOf("</td>", cellStart, StringComparison.OrdinalIgnoreCase); // Checkpoint/Endpoint
                    if (cellEnd == -1) break;

                    string cellHtml = rowHtml.Substring(cellStart, cellEnd - cellStart); // Extract

                    //cells.Add(cellHtml); // Test
                    string processedHtml = RemoveTags(cellHtml).Trim();
                    cells.Add(processedHtml);

                    cellStart = rowHtml.IndexOf("<td", cellEnd, StringComparison.OrdinalIgnoreCase); // Next cell
                }
                if (cells.Count != 0) // Empty row check
                    rows.Add(cells);

                rowStart = tableHtml.IndexOf("<tr", rowEnd, StringComparison.OrdinalIgnoreCase); // Next row
            }
            if (rows.Count > 0) // Empty table check
                table.Add(rows);

            tableStart = html.IndexOf("<table", tableEnd, StringComparison.OrdinalIgnoreCase); // Next table
        }

        return table;
    }

    // Remove '<','>', and any between
    static string RemoveTags(string processedHtml)
    {
        var stringB = new StringBuilder();
        bool inTag = false;

        foreach (char c in processedHtml)
        {
            if (c == '<') // '<' starts ignore
                inTag = true; 
            else if (c == '>') // '>' ends ignore
                inTag = false; 
            else if (!inTag) 
                stringB.Append(c); // Keep char
        }
        return stringB.ToString();
    }
}