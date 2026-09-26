namespace MmoGame3d.Tests.Data;

using MmoGame3d.Data;

public class PersistenceWorkerTests
{
    [Fact]
    public void AResultComesBackOnlyWhenCompletionsRun()
    {
        using PersistenceWorker worker = new PersistenceWorker();
        int result = 0;

        worker.Enqueue(() => 42, value => result = value, e => throw e);
        Assert.True(worker.Stop(TimeSpan.FromSeconds(5)));

        Assert.Equal(0, result);

        worker.RunCompletions();

        Assert.Equal(42, result);
    }

    [Fact]
    public void AFailureComesBackAsTheException()
    {
        using PersistenceWorker worker = new PersistenceWorker();
        Exception? failure = null;

        worker.Enqueue<int>(() => throw new InvalidOperationException("no database"), _ => { }, e => failure = e);
        worker.Stop(TimeSpan.FromSeconds(5));
        worker.RunCompletions();

        Assert.IsType<InvalidOperationException>(failure);
    }
}
