/**
 * Owned By: Chandan Srivastava
 * Created By: Chandan Srivastava
 * Date Created: 13/10/2021
 * Date Modified: 13/10/2021
**/

using System.Collections.Generic;

namespace Whiteboard
{
    /// <summary>
    ///     Interface to specify the functions handled by ServerCheckPointHandler
    /// </summary>
    public interface IServerCheckPointHandler
    {
        /// <summary>
        ///     Saves the checkpoint at the server.
        /// </summary>
        /// <param name="boardShapes">List containing all the information to save the checkpoint.</param>
        /// <param name="userId">User who requested the saving of checkpoint.</param>
        /// <returns>The number/identifier corresponding to the created checkpoint.</returns>
        int SaveCheckpoint(List<BoardShape> boardShapes, string userId);

        /// <summary>
        ///     Fetches the checkpoint corresponding to provided userId and checkPointNumber
        /// </summary>
        /// <param name="checkpointNumber">The identifier/number of the checkpoint which needs to fetched.</param>
        /// <returns>Returns list of BoardShape summarzing the checkpoint to the ServerBoardStateManager.</returns>
        List<BoardShape> FetchCheckpoint(int checkpointNumber);

        /// <summary>
        ///     To Get the total number of checkpoints saved at server side.
        /// </summary>
        /// <returns>Number corresponding to the total number of checkpoints at server.</returns>
        int GetCheckpointsNumber();
    }
}