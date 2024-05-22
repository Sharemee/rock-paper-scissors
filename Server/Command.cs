using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server;

internal class Command
{
    public bool IsValid { get; set; } = false;

    public string Code { get; set; } = null!;

    public List<string> Args { get; set; } = [];
}
