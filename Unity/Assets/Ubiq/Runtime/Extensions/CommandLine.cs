using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommandLine
{
    public static string GetArgument(string Name)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if(args[i] == Name || args[i] == "-" + Name)
            {
                return args[i + 1];
            }
        }
        return null;
    }
        
}
