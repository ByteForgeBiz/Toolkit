namespace ByteForge.Toolkit.Tests.Unit
{
    public interface IMyInterface
    {
        void MyMethod();
    }
    internal class StaticTest : IMyInterface
    {
        private static IMyInterface Instance = new StaticTest();
        public static void MyMethod() => Instance.MyMethod();
        void IMyInterface.MyMethod()
        {
            throw new NotImplementedException();
        }
    }
}
