using DigiMenuAPI.Infrastructure.Entities;

namespace DigiMenuAPI.UnitTests.TestInfrastructure.Builders;

public class BranchProductBuilder
{
    private int     _id           = 100;
    private int     _branchId     = 100;
    private int     _productId    = 100;
    private int     _categoryId   = 100;
    private decimal _price        = 1000m;
    private decimal? _offerPrice  = null;
    private int     _displayOrder = 1;
    private bool    _isVisible    = true;
    private bool    _isDeleted    = false;

    public BranchProductBuilder WithId(int id)               { _id = id;                 return this; }
    public BranchProductBuilder WithBranchId(int id)         { _branchId = id;           return this; }
    public BranchProductBuilder WithProductId(int id)        { _productId = id;          return this; }
    public BranchProductBuilder WithCategoryId(int id)       { _categoryId = id;         return this; }
    public BranchProductBuilder WithPrice(decimal price)     { _price = price;           return this; }
    public BranchProductBuilder WithOfferPrice(decimal? p)   { _offerPrice = p;          return this; }
    public BranchProductBuilder WithDisplayOrder(int order)  { _displayOrder = order;    return this; }
    public BranchProductBuilder Hidden()                     { _isVisible = false;       return this; }
    public BranchProductBuilder Deleted()                    { _isDeleted = true;        return this; }

    public BranchProduct Build() => new()
    {
        Id                  = _id,
        BranchId            = _branchId,
        ProductId           = _productId,
        CategoryId          = _categoryId,
        Price               = _price,
        OfferPrice          = _offerPrice,
        DisplayOrder        = _displayOrder,
        IsVisible           = _isVisible,
        IsDeleted           = _isDeleted,
        ImageObjectFit      = "cover",
        ImageObjectPosition = "50% 50%",
    };
}
