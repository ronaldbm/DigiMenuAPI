using AutoMapper;
using AutoMapper.QueryableExtensions;
using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.ReadDTOs;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Application.Utils;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;
using static DigiMenuAPI.Application.Common.Constants;

namespace DigiMenuAPI.Application.Services
{
    public class StoreService : IStoreService
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly LogMessageDispatcher<StoreService> logger;

        public StoreService(ApplicationDbContext context, IMapper mapper, LogMessageDispatcher<StoreService> logger)
        {
            this.context = context;
            this.mapper = mapper;
            this.logger = logger;
        }

        public async Task<OperationResult<MenuStoreDto>> GetStoreMenu()
        {
            try
            {
                // 1. Obtenemos la configuración (Settings)
                var settings = await context.Settings
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (settings is null)
                {
                    return OperationResult<MenuStoreDto>.Fail(MessageBuilder.NotFound(EntityNames.Setting));
                }

                // 2. Mapeamos la base del DTO (Branding)
                var storeMenu = mapper.Map<MenuStoreDto>(settings);

                // 3. Obtenemos las categorías primero
                var categories = await context.Categories
                    .AsNoTracking()
                    .Where(c => c.IsVisible)
                    .OrderBy(c => c.DisplayOrder)
                    .ProjectTo<CategoryReadDto>(mapper.ConfigurationProvider)
                    .ToListAsync();

                // 4. Obtenemos los footer links
                var footerLinks = await context.FooterLinks
                    .AsNoTracking()
                    .OrderBy(f => f.DisplayOrder)
                    .ProjectTo<FooterLinkReadDto>(mapper.ConfigurationProvider)
                    .ToListAsync();

                // 5. Actualizamos el objeto storeMenu usando 'with'
                // Esto crea un nuevo MenuStoreDto basado en el anterior pero con las listas llenas
                storeMenu = storeMenu with
                {
                    Categories = categories,
                    FooterLinks = footerLinks
                };

                return OperationResult<MenuStoreDto>.Ok(storeMenu);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fatal al obtener el menú completo de la tienda");
                return OperationResult<MenuStoreDto>.Fail(MessageBuilder.UnexpectedError(EntityNames.StoreMenu));
            }
        }
    }
}