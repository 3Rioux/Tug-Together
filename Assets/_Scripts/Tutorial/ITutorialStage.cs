// ITutorialStage.cs
using System;

public interface ITutorialStage
{
    event Action StageCompleted;
    void ActivateStage();
    void DeactivateStage();
}