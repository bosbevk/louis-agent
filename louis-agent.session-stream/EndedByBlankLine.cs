namespace LouisAgent.SessionStream;

/// <summary>
/// The stream with a blank line after its end. SSE discards an event the stream ends before its blank line, so a
/// connection cut off after the last event would lose it; the line-by-line reader this replaces read it, and so does
/// this. The owner of the stream still disposes it.
/// </summary>
internal sealed class EndedByBlankLine(Stream inner) : Stream
{
    private static readonly byte[] Blank = "\n\n"u8.ToArray();
    private int blankGiven;

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        int read = await inner.ReadAsync(buffer, cancellationToken);
        return read > 0 ? read : GiveBlank(buffer.Span);
    }

    public override int Read(Span<byte> buffer)
    {
        int read = inner.Read(buffer);
        return read > 0 ? read : GiveBlank(buffer);
    }

    private int GiveBlank(Span<byte> buffer)
    {
        int count = Math.Min(buffer.Length, Blank.Length - blankGiven);
        Blank.AsSpan(blankGiven, count).CopyTo(buffer);
        blankGiven += count;
        return count;
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
