using Amethystra.Diagnostics;

namespace Amethystra.Test;

[TestClass]
public sealed class AppLogDefaultTest
{
    [TestMethod]
    public void Default_Before_CreateDefault_DoesNotThrow()
    {
        var logger = AppLog.Default.For<AppLogDefaultTest>();

        logger.Info("Info");
        logger.Warn("Warn");
        logger.Error("Error");
        logger.Fatal("Fatal");

        using (logger.BeginOperation("NoOp"))
        {
            logger.Info("Inside operation");
        }
    }
}
