using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;

namespace TransMetroUMG.Infrastructure;

public sealed class CatalogosUsuariosService(MunitransDbContext dbContext)
    : ICatalogosUsuariosService
{
    public async Task<CatalogosUsuarios> ObtenerAsync(CancellationToken cancellationToken)
    {
        var municipalidades = await dbContext.Database.SqlQuery<CatalogoRegistro>($"""
            SELECT COD_MUNICIPALIDAD AS Codigo, NOMBRE_MUNICIPALIDAD AS Nombre
            FROM dbo.MUNICIPALIDAD
            ORDER BY NOMBRE_MUNICIPALIDAD, COD_MUNICIPALIDAD
            """).ToListAsync(cancellationToken);

        var roles = await dbContext.Database.SqlQuery<CatalogoRegistro>($"""
            SELECT COD_ROL AS Codigo, NOMBRE_ROL AS Nombre
            FROM dbo.ROL
            ORDER BY NOMBRE_ROL, COD_ROL
            """).ToListAsync(cancellationToken);

        return new CatalogosUsuarios(
            municipalidades.Select(x => new OpcionCatalogo(x.Codigo, x.Nombre)).ToList(),
            roles.Select(x => new OpcionCatalogo(x.Codigo, x.Nombre)).ToList());
    }

    private sealed class CatalogoRegistro
    {
        public int Codigo { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }
}
