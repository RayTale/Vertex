using System.Threading.Tasks;

namespace Vertex.Utils.Channels
{
    public class AskInputBox<TInput, TOutput>
    {
        public AskInputBox(TInput data)
        {
            this.Value = data;
        }

        public TaskCompletionSource<TOutput> TaskSource { get; } = new TaskCompletionSource<TOutput>(TaskCreationOptions.RunContinuationsAsynchronously);

        public TInput Value { get; set; }
    }
}
