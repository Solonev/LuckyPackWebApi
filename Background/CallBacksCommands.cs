namespace LuckyPackWebApi.Background;

public enum CallBacksCommands
{
    Lk,
    MainMenu,
    Payment,
    Undefined,
    Catalog,
    KnowledgeBase,
    SetUserPhone,
    SetUserEmail
}

public enum ChatState
{
    Main,
    WaitEmail,
    WaitPhone
}