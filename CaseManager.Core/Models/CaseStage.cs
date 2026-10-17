namespace CaseManager.Core.Models;

public enum CaseStage
{
    Started,     // старт
    InProgress,  // идёт процесс
    Appeal,      // апелляция
    Cassation,   // кассация
    Closed       // завершено
}