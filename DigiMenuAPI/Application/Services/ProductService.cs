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
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly LogMessageDispatcher<ProductService> logger;

        public ProductService(ApplicationDbContext context, IMapper mapper, LogMessageDispatcher<ProductService> logger)
        {
            this.context = context;
            this.mapper = mapper;
            this.logger = logger;
        }

        #region Create
        public async Task<OperationResult<int>> Create(ProductCreateDto productDto)
        {
            try
            {
                bool exists = await context.Product
                                                    .AnyAsync(p => p.Label == productDto.Label && p.Alive);

                if (exists)
                {
                    logger.LogWarning(MessageBuilder.AlreadyExists(EntityNames.Product), productDto);
                    return OperationResult<int>.Fail(MessageBuilder.AlreadyExists(EntityNames.Product));
                }

                // Calcula la posición
                int nextPosition = await context.Product
                                                        .Where(p => p.Alive)
                                                        .CountAsync();

                productDto.Position = nextPosition + 1;

                // Mapea el DTO a la entidad
                var product = mapper.Map<Product>(productDto);
                product.Alive = true;

                //Guardar
                await context.Product.AddAsync(product);
                await context.SaveChangesAsync();

                logger.LogCreate(EntityNames.Product, product);
                return OperationResult<int>.Ok(product.Id, MessageBuilder.Created(EntityNames.Product));

            }
            catch (Exception ex)
            {
                logger.LogError(ex,MessageBuilder.UnexpectedError(EntityNames.Product), productDto);
                return OperationResult<int>.Fail(MessageBuilder.UnexpectedError(EntityNames.Product));
            }
        }


        #endregion

        #region Updates
        public async Task<OperationResult<bool>> Delete(int Id)
        {
            try
            {
                var product = context.Product.FirstOrDefault(p => p.Id == Id && p.Alive);
            
                if (product == null) //No se ha encontrado el Id
                {
                    logger.LogWarning(MessageBuilder.NotFound(EntityNames.Product), Id);
                    return OperationResult<bool>.Fail(MessageBuilder.NotFound(EntityNames.Product)); 
                }

                //Modificarle el estado
                product.Alive = false;
              
                //Coloco la posición de todos los demas productos correctamente
                await context.Product
                                    .Where(p => p.Position >= product.Position)
                                    .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.Position, p => p.Position - 1));

                //Verifico que si se hayan hecho los cambios
                await context.SaveChangesAsync();

                logger.LogDelete(EntityNames.Product, product);
                var result = OperationResult<bool>.Ok(true, MessageBuilder.Deleted(EntityNames.Product));

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, MessageBuilder.UnexpectedError(EntityNames.Product), Id);
                return OperationResult<bool>.Fail(MessageBuilder.UpdatedError(EntityNames.Product));
            }


        }

        public async Task<OperationResult<bool>> Update(ProductUpdateDto productDto)
        {
            try
            {
                bool existsProduct = context
                                            .Product
                                            .Any(p => (p.Label == productDto.Label && p.Id != productDto.Id) && p.Alive);

                if (existsProduct)
                {
                    logger.LogWarning(MessageBuilder.AlreadyExists(EntityNames.Product), productDto);
                    return OperationResult<bool>.Fail(MessageBuilder.AlreadyExists(EntityNames.Product));
                }

                //Obtengo el producto que se ha modificado según el Id y le coloco los datos nuevos
                var product = await context.Product.FirstOrDefaultAsync(p => p.Id == productDto.Id && p.Alive);
                mapper.Map(productDto, product);


                //Guardo los cambios
                await context.SaveChangesAsync();

                logger.LogUpdate(EntityNames.Product,productDto);
                var result = OperationResult<bool>.Ok(true, MessageBuilder.Updated(EntityNames.Product));

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,MessageBuilder.UnexpectedError(EntityNames.Product), productDto);
                return OperationResult<bool>.Fail(MessageBuilder.UpdatedError(EntityNames.Product));

            }
        }

        public async Task<OperationResult<bool>> UpdatePosition(ItemPositionUpdate productDto)
        {
            try
            {
                var movedProduct = context.Product.FirstOrDefault(p => p.Id == productDto.Id);
                if (movedProduct == null)
                {
                    logger.LogWarning(
                        MessageBuilder.NotFound(EntityNames.Product),
                        productDto
                    ); 
                    return OperationResult<bool>.Fail(MessageBuilder.NotFound(EntityNames.Product));
                }

                int oldPosition = movedProduct.Position;
                int newPosition = productDto.Position;
                int maxPosition = context.Product.Count(p => p.Alive);

                //Validar si las posiciones son validas
                if (maxPosition < newPosition || newPosition <= 0)
                {
                    logger.LogWarning(MessageBuilder.PositionInvalid(EntityNames.Product), movedProduct);
                    return OperationResult<bool>.Fail(MessageBuilder.PositionInvalid(EntityNames.Product));
                }

                if (newPosition == oldPosition) //No se debe de actualizar nada
                {
                    return OperationResult<bool>.Ok(true);
                }

                //Verificar si el item subio o bajo de posición
                if (newPosition < oldPosition)
                {
                    await context.Product
                                        .Where(p => p.Position >= newPosition && p.Position < oldPosition)
                                        .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.Position, p => p.Position + 1));
                }
                else
                {
                    await context.Product
                        .Where(p => p.Position > oldPosition && p.Position <= newPosition)
                        .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.Position, p => p.Position - 1));
                }

                //Guardo la nueva posición
                movedProduct.Position = newPosition;

                int affected = await context.SaveChangesAsync();

                OperationResult<bool> result;
                if (affected > 0) {
                    logger.LogUpdate(EntityNames.Product,movedProduct);
                    result = OperationResult<bool>.Ok(true, MessageBuilder.Updated(EntityNames.Product));
                }
                else
                {
                    logger.LogWarning(MessageBuilder.UnexpectedError(EntityNames.Product), movedProduct);
                    result = OperationResult<bool>.Fail(MessageBuilder.UpdatedError(EntityNames.Product));
                }

                return result;
                
            }
            catch (Exception ex)
            {
                logger.LogError(ex, MessageBuilder.UnexpectedError(EntityNames.Product),productDto);
                return OperationResult<bool>.Fail(MessageBuilder.UpdatedError(EntityNames.Product));
            }
        }

        #endregion Updates


        #region Read
        public async Task<List<ProductDto>> GetAll()
        {
            var productList = await  context   
                                            .Product
                                            .AsNoTracking()
                                            .Where(p => p.Alive)
                                            .ProjectTo<ProductDto>(mapper.ConfigurationProvider)
                                            .ToListAsync();

            return productList;
        }

        public async Task<ProductUpdateDto?> GetOne(int Id)
        {
            var product = await context
                                        .Product
                                        .AsNoTracking()
                                        .Where(p => p.Id == Id && p.Alive)
                                        .ProjectTo<ProductUpdateDto>(mapper.ConfigurationProvider)
                                        .FirstOrDefaultAsync();
            return product;
        }
        #endregion Read
    }
}
