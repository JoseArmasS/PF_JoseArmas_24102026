namespace TransMetroUMG.Application;

public sealed record UsuarioAutenticado(int Codigo, string Nombre);

public interface IAutenticacionService
{
    Task<UsuarioAutenticado?> ValidarAsync(string nombre, string contrasena, CancellationToken cancellationToken);
    Task<bool> SigueVigenteAsync(int codigo, CancellationToken cancellationToken);
}
