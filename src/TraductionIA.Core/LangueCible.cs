namespace TraductionIA.Core
{
    /// <summary>
    /// Langue cible d'une traduction. La valeur porte le nom de langue qui sera
    /// inséré dans la consigne envoyée à l'IA (voir <see cref="OpenAiTranslator"/>).
    /// </summary>
    public enum LangueCible
    {
        Anglais,
        Espagnol,
        Chinois,
        Japonais
    }
}
