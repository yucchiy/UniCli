using ConsoleAppFramework;
using System;
using System.Threading.Tasks;

namespace UniCli.Client;

public partial class Commands
{
    /// <summary>
    /// Output shell completion script for the specified shell
    /// </summary>
    public Task<int> Completions([Argument] string shell = "bash")
    {
        var script = shell.ToLowerInvariant() switch
        {
            "bash" => BashScript,
            "zsh" => ZshScript,
            "fish" => FishScript,
            _ => null
        };

        if (script == null)
        {
            Console.Error.WriteLine($"Unsupported shell: {shell}. Supported: bash, zsh, fish");
            return Task.FromResult(1);
        }

        Console.Write(script);
        return Task.FromResult(0);
    }

    private const string BashScript = @"_unicli_completions() {
    local cur_line=""${COMP_LINE}""
    local candidates
    candidates=$(unicli complete ""$cur_line"" 2>/dev/null)
    COMPREPLY=( $(compgen -W ""$candidates"" -- ""${COMP_WORDS[COMP_CWORD]}"") )
}
complete -F _unicli_completions unicli
";

    private const string ZshScript = @"_unicli_completions() {
    local -a candidates
    candidates=(""${(@f)$(unicli complete ""${words[*]}"" 2>/dev/null)}"")
    compadd -a candidates
}
compdef _unicli_completions unicli
";

    private const string FishScript = @"complete -c unicli -f -a '(unicli complete (commandline -cp) 2>/dev/null)'
";
}
