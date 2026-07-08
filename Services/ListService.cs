using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace ZapretGUI.Services;

public class ParsedLists
{
    public List<string> IpSetAll { get; } = new();
    public List<string> GoogleDomains { get; } = new();
    public List<string> GeneralDomains { get; } = new();
    public List<string> InvalidLines { get; } = new();
}

public class ListService
{
    private readonly string _zapretFolder;
    private readonly string _listsFolder;

    private static readonly Regex CidrRegex = new(@"^[0-9A-Fa-f:.]+\/\d{1,3}$", RegexOptions.Compiled);
    private static readonly Regex DomainRegex = new(@"^[a-zA-Z0-9.-]+$", RegexOptions.Compiled);

    public ListService(string zapretFolder)
    {
        _zapretFolder = zapretFolder;
        _listsFolder = Path.Combine(_zapretFolder, "lists");
    }

    public ParsedLists Parse(string rawInput)
    {
        var result = new ParsedLists();

        var googleReference = LoadGoogleReference();

        var lines = rawInput.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.StartsWith('#') || line.StartsWith(';'))
                continue;

            if (IsIpOrCidr(line))
            {
                result.IpSetAll.Add(line);
                continue;
            }

            if (IsDomain(line))
            {
                if (IsGoogleDomain(line, googleReference))
                {
                    result.GoogleDomains.Add(line);
                }
                else
                {
                    result.GeneralDomains.Add(line);
                }
                continue;
            }

            result.InvalidLines.Add(line);
        }

        return result;
    }

    private HashSet<string> LoadGoogleReference()
    {
        var path = Path.Combine(_listsFolder, "list-google.txt");
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            if (!File.Exists(path))
                return set;

            foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#') || trimmed.StartsWith(';'))
                    continue;
                set.Add(trimmed);
            }
        }
        catch
        {
            // при ошибке вернем то, что уже есть
        }

        return set;
    }

    private bool IsIpOrCidr(string value)
    {
        if (IPAddress.TryParse(value, out _))
            return true;

        if (CidrRegex.IsMatch(value))
        {
            var slashIndex = value.IndexOf('/');
            if (slashIndex > 0)
            {
                var ipPart = value[..slashIndex];
                return IPAddress.TryParse(ipPart, out _);
            }
        }

        return false;
    }

    private bool IsDomain(string value)
    {
        if (!DomainRegex.IsMatch(value))
            return false;

        // Должна быть хотя бы одна точка
        return value.Contains('.');
    }

    private bool IsGoogleDomain(string domain, HashSet<string> googleReference)
    {
        foreach (var entry in googleReference)
        {
            if (domain.Equals(entry, StringComparison.OrdinalIgnoreCase))
                return true;

            if (domain.EndsWith("." + entry, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public void Apply(ParsedLists parsed)
    {
        if (!Directory.Exists(_listsFolder))
            throw new DirectoryNotFoundException($"Папка lists не найдена: {_listsFolder}");

        ApplyToFile(Path.Combine(_listsFolder, "ipset-all.txt"), parsed.IpSetAll);
        ApplyToFile(Path.Combine(_listsFolder, "list-google.txt"), parsed.GoogleDomains);
        ApplyToFile(Path.Combine(_listsFolder, "list-general.txt"), parsed.GeneralDomains);
    }

    private int ApplyToFile(string targetFilePath, List<string> newLines)
    {
        if (newLines.Count == 0)
            return 0;

        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        var allLines = new List<string>();

        if (File.Exists(targetFilePath))
        {
            allLines.AddRange(File.ReadAllLines(targetFilePath, encoding));
        }

        var existingSet = new HashSet<string>(allLines.Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim()), StringComparer.OrdinalIgnoreCase);

        var added = 0;
        foreach (var line in newLines)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
                continue;

            if (!existingSet.Contains(trimmed))
            {
                existingSet.Add(trimmed);
                allLines.Add(trimmed);
                added++;
            }
        }

        // Нечего добавлять — файл не трогаем.
        if (added == 0)
            return 0;

        var tempPath = targetFilePath + ".tmp";
        var backupPath = targetFilePath + ".bak";

        try
        {
            File.WriteAllLines(tempPath, allLines, encoding);

            if (File.Exists(targetFilePath))
            {
                // атомарная замена с резервной копией
                File.Replace(tempPath, targetFilePath, backupPath, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(tempPath, targetFilePath);
            }
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch
            {
                // best-effort cleanup
            }
        }

        return added;
    }

    /// <summary>
    /// Проверяет и дописывает IP/CIDR-строки в lists/ipset-all.txt
    /// (с дедупликацией, атомарной записью и бэкапом). Возвращает число новых записей.
    /// </summary>
    public int MergeIpSet(IEnumerable<string> rawLines)
    {
        if (!Directory.Exists(_listsFolder))
            Directory.CreateDirectory(_listsFolder);

        var valid = new List<string>();
        foreach (var raw in rawLines)
        {
            var line = raw.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#') || line.StartsWith(';'))
                continue;

            if (IsIpOrCidr(line))
                valid.Add(line);
        }

        if (valid.Count == 0)
            return 0;

        return ApplyToFile(Path.Combine(_listsFolder, "ipset-all.txt"), valid);
    }
}

