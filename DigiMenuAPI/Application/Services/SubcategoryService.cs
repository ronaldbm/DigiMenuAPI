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
    public class SubcategoryService : ISubcategoryService
    {

        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly LogMessageDispatcher<SubcategoryService> logger;

        public SubcategoryService(ApplicationDbContext context, IMapper mapper, LogMessageDispatcher<SubcategoryService> logger)
        {
            this.context = context;
            this.mapper = mapper;
            this.logger = logger;
        }

        #region Create
        public async Task<OperationResult<int>> Create(SubcategoryCreateDto subcategoryDto)
        {
            try
            {
                bool exists = await context.Subcategory
                                                    .AnyAsync(c => c.Label == subcategoryDto.Label && c.Alive);

                if (exists)
                {
                    logger.LogWarning(MessageBuilder.AlreadyExists(EntityNames.Subcategory), subcategoryDto);
                    return OperationResult<int>.Fail(MessageBuilder.AlreadyExists(EntityNames.Subcategory));
                }

                // Calcula la posición
                int nextPosition = await context.Subcategory
                                                        .Where(c => c.Alive)
                                                        .CountAsync();

                subcategoryDto.Position = nextPosition + 1;

                // Mapea el DTO a la entidad
                var subcategory = mapper.Map<Subcategory>(subcategoryDto);
                subcategory.Alive = true;

                //Guardar
                await context.Subcategory.AddAsync(subcategory);
                await context.SaveChangesAsync();

                logger.LogCreate(EntityNames.Subcategory, subcategory);
                return OperationResult<int>.Ok(subcategory.Id, MessageBuilder.Created(EntityNames.Subcategory));

            }
            catch (Exception ex)
            {
                logger.LogError(ex, MessageBuilder.UnexpectedError(EntityNames.Subcategory), subcategoryDto);
                return OperationResult<int>.Fail(MessageBuilder.UnexpectedError(EntityNames.Subcategory));
            }
        }

        #endregion Create

        #region Updates
        public async Task<OperationResult<bool>> Delete(int Id)
        {
            try
            {
                var subcategory = context.Subcategory.FirstOrDefault(c => c.Id == Id);

                if (subcategory == null) //No se ha encontrado el Id
                {
                    logger.LogWarning(MessageBuilder.NotFound(EntityNames.Subcategory), Id);
                    return OperationResult<bool>.Fail(MessageBuilder.NotFound(EntityNames.Subcategory));
                }

                //Modificarle el estado
                subcategory.Alive = false;

                //Coloco la posición de todas las demas subcategorías correctamente
                await context.Subcategory
                                    .Where(c => c.Position <= subcategory.Position)
                                    .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.Position, c => c.Position - 1));

                //Verifico que si se hayan hecho los cambios
                await context.SaveChangesAsync();

                logger.LogDelete(EntityNames.Subcategory, subcategory);
                var result = OperationResult<bool>.Ok(true, MessageBuilder.Deleted(EntityNames.Subcategory));

                return result;

            }
            catch (Exception ex)
            {
                logger.LogError(ex, MessageBuilder.UnexpectedError(EntityNames.Subcategory), Id);
                return OperationResult<bool>.Fail(MessageBuilder.UpdatedError(EntityNames.Subcategory));
            }
        }

        public async Task<OperationResult<bool>> Update(SubcategoryUpdateDto subcategoryDto)
        {
            try
            {
                bool existsProduct = context
                                            .Subcategory
                                            .Any(c => (c.Label == subcategoryDto.Label && c.Id != subcategoryDto.Id) && c.Alive);

                if (existsProduct)
                {
                    logger.LogWarning(MessageBuilder.AlreadyExists(EntityNames.Subcategory), subcategoryDto);
                    return OperationResult<bool>.Fail(MessageBuilder.AlreadyExists(EntityNames.Subcategory));
                }

                //Obtengo la categoría que se ha modificado según el Id y le coloco los datos nuevos
                var subcategory = await context.Subcategory.FirstOrDefaultAsync(c => c.Id == subcategoryDto.Id && c.Alive);
                mapper.Map(subcategoryDto, subcategory);

                //Guardo los cambios
                await context.SaveChangesAsync();

                logger.LogUpdate(EntityNames.Subcategory, subcategoryDto);
                var result = OperationResult<bool>.Ok(true, MessageBuilder.Updated(EntityNames.Subcategory));

                return result;

            }
            catch (Exception ex)
            {
                logger.LogError(ex, MessageBuilder.UnexpectedError(EntityNames.Subcategory), subcategoryDto);
                return OperationResult<bool>.Fail(MessageBuilder.UpdatedError(EntityNames.Subcategory));
            }
        }

        public async Task<OperationResult<bool>> UpdatePosition(ItemPositionUpdate subcategoryDto)
        {
            try
            {
                var movedCategory = context.Subcategory.FirstOrDefault(c => c.Id == subcategoryDto.Id);
                if (movedCategory == null)
                {
                    logger.LogWarning(
                        MessageBuilder.NotFound(EntityNames.Subcategory),
                        subcategoryDto
                    );
                    return OperationResult<bool>.Fail(MessageBuilder.NotFound(EntityNames.Subcategory));
                }

                int oldPosition = movedCategory.Position;
                int newPosition = subcategoryDto.Position;
                int maxPosition = context.Subcategory.Count(c => c.Alive);

                //Validar si las posiciones son validas
                if (maxPosition < newPosition || newPosition <= 0)
                {
                    logger.LogWarning(MessageBuilder.PositionInvalid(EntityNames.Subcategory), movedCategory);
                    return OperationResult<bool>.Fail(MessageBuilder.PositionInvalid(EntityNames.Subcategory));
                }

                if (newPosition == oldPosition) //No se debe de actualizar nada
                {
                    return OperationResult<bool>.Ok(true);
                }

                //Verificar si el item subio o bajo de posición
                if (newPosition < oldPosition)
                {
                    await context.Subcategory
                                        .Where(c => c.Position >= newPosition && c.Position < oldPosition)
                                        .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.Position, c => c.Position + 1));
                }
                else
                {
                    await context.Subcategory
                        .Where(c => c.Position > oldPosition && c.Position <= newPosition)
                        .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.Position, c => c.Position - 1));
                }

                //Guardo la nueva posición
                movedCategory.Position = newPosition;

                int affected = await context.SaveChangesAsync();

                OperationResult<bool> result;
                if (affected > 0)
                {
                    logger.LogUpdate(EntityNames.Subcategory, movedCategory);
                    result = OperationResult<bool>.Ok(true, MessageBuilder.Updated(EntityNames.Subcategory));
                }
                else
                {
                    logger.LogWarning(MessageBuilder.UnexpectedError(EntityNames.Subcategory), movedCategory);
                    result = OperationResult<bool>.Fail(MessageBuilder.UpdatedError(EntityNames.Subcategory));
                }

                return result;

            }
            catch (Exception ex)
            {
                logger.LogError(ex, MessageBuilder.UnexpectedError(EntityNames.Subcategory), subcategoryDto);
                return OperationResult<bool>.Fail(MessageBuilder.UpdatedError(EntityNames.Subcategory));
            }
        }

        #endregion Updates


        #region Read
        public async Task<List<SubcategoryDto>> GetAll()
        {
            var subcategoryList = await context
                                            .vwGetAllSubcategories
                                            .AsNoTracking()
                                            .OrderBy(c => c.Position)
                                            .ProjectTo<SubcategoryDto>(mapper.ConfigurationProvider)
                                            .ToListAsync();

            return subcategoryList;
        }

        public async Task<SubcategoryDto?> GetOne(int Id)
        {
            var subcategory = await context
                                        .Subcategory
                                        .AsNoTracking()
                                        .Where(c => c.Id == Id && c.Alive)
                                        .ProjectTo<SubcategoryDto>(mapper.ConfigurationProvider)
                                        .FirstOrDefaultAsync();
            return subcategory;
        }
        #endregion Read
    }
}
