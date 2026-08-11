namespace Terrenario.Api.Domain.Masters;

/// <summary>
/// Los cuatro maestros de la épica MVP-002 sobre los que MVP-806 permite depurar: borrar lo que nunca
/// se usó y fusionar lo que quedó partido en dos.
///
/// Es un enum y no cuatro caminos paralelos porque la parte delicada —comprobar el «sin uso» contra
/// <b>todas</b> las entidades que pueden referenciar al registro— es la misma operación en los cuatro
/// y solo cambia la lista de referencias. Tenerla en un único sitio (<c>MasterReferenceMap</c>) es lo
/// que impide el fallo que describe el spec: comprobar contra una sola tabla y dejar un registro
/// operativo huérfano.
/// </summary>
public enum MasterKind
{
    Plot,
    Season,
    Worker,
    Task
}

/// <summary>Nombres de los maestros tal y como aparecen en los mensajes de error del contrato.</summary>
public static class MasterKinds
{
    /// <summary>Singular, en minúscula: «No se puede eliminar el <b>terreno</b>…».</summary>
    public static string Singular(MasterKind kind) => kind switch
    {
        MasterKind.Plot => "terreno",
        MasterKind.Season => "temporada",
        MasterKind.Worker => "trabajador",
        MasterKind.Task => "tarea",
        _ => "registro"
    };

    /// <summary>Artículo determinado que le corresponde, para que el mensaje concuerde en género.</summary>
    public static string Article(MasterKind kind) => kind switch
    {
        MasterKind.Season or MasterKind.Task => "la",
        _ => "el"
    };

    /// <summary>
    /// Pronombre de objeto directo: «…2 actividades <b>lo</b> referencian». Existe por lo mismo que
    /// <see cref="Article"/>: un mensaje que el usuario lee no puede estar mal concordado.
    /// </summary>
    public static string ObjectPronoun(MasterKind kind) => kind switch
    {
        MasterKind.Season or MasterKind.Task => "la",
        _ => "lo"
    };
}
