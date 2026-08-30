namespace Abstract.Data;

[Flags]
public enum Parameters
{
    None = 0,
    Description = 1 << 0,
    User = 1 << 1,
    Location = 1 << 2,
    Music = 1 << 3
}