using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Plugins;

public class TimePlugin
{
  [KernelFunction, Description("Get the current date and time")]
  public static DateTime Time()
  {
    return DateTime.UtcNow;
  }
}
