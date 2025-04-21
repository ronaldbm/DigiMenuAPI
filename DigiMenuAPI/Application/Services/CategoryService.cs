using AutoMapper;
using AutoMapper.QueryableExtensions;
using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.AddDTOs;
using DigiMenuAPI.Application.DTOs.ReadDTOs;
using DigiMenuAPI.Application.DTOs.UpdateDTOs;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Application.Utils;
using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;
using static DigiMenuAPI.Application.Common.Constants;

namespace DigiMenuAPI.Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly LogMessageDispatcher<CategoryService> logger;

        public CategoryService(ApplicationDbContext context, IMapper mapper, LogMessageDispatcher<CategoryService> logger)
        {
            this.context = context;
            this.mapper = mapper;
            this.logger = logger;
        }

        #region Create
        public async Task<OperationResult<int>> Create(CategoryCreateDto categoryDto)
        {
            try
            {
                bool exists = await context.Category
                                                    .AnyAsync(c => c.Label == categoryDto.Label && c.Alive);

                if (exists)
                {
                    logger.LogWarning(MessageBuilder.AlreadyExists(EntityNames.Category), categoryDto);
                    return OperationResult<int>.Fail(MessageBuilder.AlreadyExists(EntityNames.Category));
                }

                // Calcula la posición
                int nextPosition = await context.Category
                                                        .Where(c => c.Alive)
                                                        .CountAsync();

                categoryDto.Position = nextPosition + 1;

                // Mapea el DTO a la entidad
                var category = mapper.Map<Category>(categoryDto);
                category.Alive = true;

                //Guardar
                await context.Category.AddAsync(category);
                await context.SaveChangesAsync();

                logger.LogCreate(EntityNames.Category, category);
                return OperationResult<int>.Ok(category.Id, MessageBuilder.Created(EntityNames.Category));

            }
            catch (Exception ex)
            {
                logger.LogError(ex, MessageBuilder.UnexpectedError(EntityNames.Category), categoryDto);
                return OperationResult<int>.Fail(MessageBuilder.UnexpectedError(EntityNames.Category));
            }
        }

        #endregion Create

        #region Updates
        public async Task<OperationResult<bool>> Delete(int Id)
        {
            try
            {
                var category = context.Category.FirstOrDefault(c => c.Id == Id);

                if (category == null) //No se ha encontrado el Id
                {
                    logger.LogWarning(MessageBuilder.NotFound(EntityNames.Category), Id);
                    return OperationResult<bool>.Fail(MessageBuilder.NotFound(EntityNames.Category));
                }

                //Modificarle el estado
                category.Alive = false;

                //Coloco la posición de todas las demas categorías correctamente
                await context.Category
                                    .Where(c => c.Position >= category.Position)
                                    .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.Position, c => c.Position - 1));

                //Verifico que si se hayan hecho los cambios
                 await context.SaveChangesAsync();
  
                logger.LogDelete(EntityNames.Category, category);
                var result = OperationResult<bool>.Ok(true, MessageBuilder.Deleted(EntityNames.Category));

                return result;

            }
            catch (Exception ex)
            {
                logger.LogError(ex, MessageBuilder.UnexpectedError(EntityNames.Category), Id);
                return OperationResult<bool>.Fail(MessageBuilder.UpdatedError(EntityNames.Category));
            }
        }

        public async Task<OperationResult<bool>> Update(CategoryUpdateDto categoryDto)
        {
            try
            {
                bool existsProduct = context
                                            .Category
                                            .Any(c => (c.Label == categoryDto.Label && c.Id != categoryDto.Id) && c.Alive);

                if (existsProduct)
                {
                    logger.LogWarning(MessageBuilder.AlreadyExists(EntityNames.Category), categoryDto);
                    return OperationResult<bool>.Fail(MessageBuilder.AlreadyExists(EntityNames.Category));
                }

                //Obtengo la categoría que se ha modificado según el Id y le coloco los datos nuevos
                var category = await context.Category.FirstOrDefaultAsync(c => c.Id == categoryDto.Id && c.Alive);
                mapper.Map(categoryDto, category);

                //Guardo los cambios
                await context.SaveChangesAsync();

                logger.LogUpdate(EntityNames.Category, categoryDto);
                var result = OperationResult<bool>.Ok(true, MessageBuilder.Updated(EntityNames.Category));
 
                return result;

            }
            catch (Exception ex)
            {
                logger.LogError(ex, MessageBuilder.UnexpectedError(EntityNames.Category), categoryDto);
                return OperationResult<bool>.Fail(MessageBuilder.UpdatedError(EntityNames.Category));
            }
        }

        public async Task<OperationResult<bool>> UpdatePosition(ItemPositionUpdate categoryDto)
        {
            try
            {
                var movedCategory = context.Category.FirstOrDefault(c => c.Id == categoryDto.Id);
                if (movedCategory == null)
                {
                    logger.LogWarning(
                        MessageBuilder.NotFound(EntityNames.Category),
                        categoryDto
                    );
                    return OperationResult<bool>.Fail(MessageBuilder.NotFound(EntityNames.Category));
                }

                int oldPosition = movedCategory.Position;
                int newPosition = categoryDto.Position;
                int maxPosition = context.Category.Count(c => c.Alive);

                //Validar si las posiciones son validas
                if (maxPosition < newPosition || newPosition <= 0)
                {
                    logger.LogWarning(MessageBuilder.PositionInvalid(EntityNames.Category), movedCategory);
                    return OperationResult<bool>.Fail(MessageBuilder.PositionInvalid(EntityNames.Category));
                }

                if (newPosition == oldPosition) //No se debe de actualizar nada
                {
                    return OperationResult<bool>.Ok(true);
                }

                //Verificar si el item subio o bajo de posición
                if (newPosition < oldPosition)
                {
                    await context.Category
                                        .Where(c => c.Position >= newPosition && c.Position < oldPosition)
                                        .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.Position, c => c.Position + 1));
                }
                else
                {
                    await context.Category
                        .Where(c => c.Position > oldPosition && c.Position <= newPosition)
                        .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.Position, c => c.Position - 1));
                }

                //Guardo la nueva posición
                movedCategory.Position = newPosition;

                int affected = await context.SaveChangesAsync();

                OperationResult<bool> result;
                if (affected > 0)
                {
                    logger.LogUpdate(EntityNames.Category, movedCategory);
                    result = OperationResult<bool>.Ok(true, MessageBuilder.Updated(EntityNames.Category));
                }
                else
                {
                    logger.LogWarning(MessageBuilder.UnexpectedError(EntityNames.Category), movedCategory);
                    result = OperationResult<bool>.Fail(MessageBuilder.UpdatedError(EntityNames.Category));
                }

                return result;

            }
            catch (Exception ex)
            {
                logger.LogError(ex, MessageBuilder.UnexpectedError(EntityNames.Category), categoryDto);
                return OperationResult<bool>.Fail(MessageBuilder.UpdatedError(EntityNames.Category));
            }
        }

        #endregion Updates


        #region Read
        public async Task<List<CategoryDto>> GetAll()
        {
            var categoryList = await context
                                            .Category
                                            .AsNoTracking()
                                            .Where(c => c.Alive)
                                            .ProjectTo<CategoryDto>(mapper.ConfigurationProvider)
                                            .ToListAsync();

            return categoryList;
        }

        public async Task<CategoryDto?> GetOne(int Id)
        {
            var category = await context
                                        .Category
                                        .AsNoTracking()
                                        .Where(c => c.Id == Id && c.Alive)
                                        .ProjectTo<CategoryDto>(mapper.ConfigurationProvider)
                                        .FirstOrDefaultAsync();
            return category;
        }
        #endregion Read
    }
}
