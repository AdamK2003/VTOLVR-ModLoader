public class Versions
{
    public int currentDLLVersion;
    public int currentEXEVersion;
    public Versions() { }

    public Versions(int currentDLL, int currentExe)
    {
        currentDLLVersion = currentDLL;
        currentEXEVersion = currentExe;
    }
}