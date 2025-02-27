using UnityEngine;
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

  public static float GaussianRandom(float mean, float stdDev)
  {
    float u1 = 1.0f - Random.value; // Uniform random number between 0 and 1
    float u2 = 1.0f - Random.value; // Uniform random number between 0 and 1
    float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2); // Random number with standard normal distribution
    return mean + stdDev * randStdNormal; // Scale and shift to get the desired mean and standard deviation
  }
}