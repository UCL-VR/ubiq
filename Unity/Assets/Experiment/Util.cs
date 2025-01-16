using System.IO;

public static class Util
{
  public static void WriteToFile(string filePath, string content)
  {
    string directory = Path.GetDirectoryName(filePath);
    if (!Directory.Exists(directory))
    {
      Directory.CreateDirectory(directory);
    }
    using (StreamWriter writer = new StreamWriter(filePath, true))
    {
      writer.WriteLine(content);
    }
  }
}