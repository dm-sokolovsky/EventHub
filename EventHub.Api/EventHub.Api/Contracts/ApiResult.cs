using System.Net;

namespace EventHub.Api.Models;

/// <summary>
/// Класс с возвращаемыми данными
/// </summary>
/// <typeparam name="T"></typeparam>
public class ApiResult<T> : ApiBaseResult
{
    /// <summary>
    /// Возвращаемые данные метода
    /// </summary>
    public required T Data { get; set; }
}

/// <summary>
/// Класс ApiResult без возвращаемых данных
/// Наследуемся от базового класса с основными параметрами
/// </summary>
public class ApiResult : ApiBaseResult { }


/// <summary>
/// Базовый класс с основными возвращаемыми параметрами
/// </summary>
public class ApiBaseResult
{
    /// <summary>
    /// Флаг, указывающий на успешность выполненного запроса
    /// </summary>
    public required bool Success { get; set; }
    /// <summary>
    /// Возвращаемый HTTP-код
    /// </summary>
    public required HttpStatusCode StatusCode { get; set; }
    /// <summary>
    /// Дата и время ответа
    /// </summary>
    public DateTime DateTime { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Кастомное сообщение с дополнительной информацией
    /// Здесь может быть информация об ошибке в случае неуспеха
    /// </summary>
    public required string Message { get; set; }
} 