namespace StudioDrydock.AppStoreConnect.ApiGenerator;

internal class CsWriter
{
    private readonly TextWriter writer;
    private int indent = 0;
    private bool commaRequired;

    public CsWriter(TextWriter writer)
    {
        this.writer = writer;
    }

    public void BeginLine()
    {
        for (var i = 0; i < indent; ++i)
            writer.Write("    ");
    }

    public void EndLine() => writer.WriteLine();

    public void Write(string text) => writer.Write(text);

    public void WriteLine(string line)
    {
        BeginLine();
        writer.WriteLine(line);
    }

    public void WriteLine() => writer.WriteLine();

    public void Comment(string comment)
    {
        WriteLine($"// {comment}");
    }

    public void BeginBlock()
    {
        WriteLine("{");
        ++indent;
    }

    public void BeginBlock(string line)
    {
        WriteLine(line);
        WriteLine("{");
        ++indent;
    }

    public void EndBlock(bool trailingNewLine = true)
    {
        --indent;
        WriteLine("}");
        if (trailingNewLine)
            WriteLine();
    }

    public void BeginCommaDelimitedList()
    {
        commaRequired = false;
    }

    public void WriteCommaIfRequired()
    {
        if (commaRequired)
            Write(", ");
        commaRequired = true;
    }
}