const apiExamples = {
  auth: {
    request: {
      method: "POST",
      path: "/api/auth/sync",
      headers: {
        Authorization: "Bearer <firebase-id-token>",
      },
      body: {
        role: "customer",
      },
    },
    response: {
      success: true,
      message: "User synchronized successfully",
      data: {
        id: 1,
        firebaseUid: "firebase_uid",
        email: "user@example.com",
        role: "customer",
      },
    },
  },
  users: {
    request: {
      method: "PATCH",
      path: "/api/users/me",
      body: {
        displayName: "Areeba Khan",
        phone: "+923001234567",
      },
    },
    response: {
      success: true,
      message: "User profile updated successfully",
    },
  },
  vendors: {
    request: {
      method: "POST",
      path: "/api/vendors/profile",
      body: {
        companyName: "Urban Living Studio",
        description: "Vendor profile for curated furniture listings.",
      },
    },
    response: {
      success: true,
      message: "Vendor profile created successfully",
    },
  },
  products: {
    request: {
      method: "POST",
      path: "/api/products",
      body: {
        categoryId: 1,
        name: "Scandinavian Sofa",
        brandName: "Urban Living Studio",
        price: 252000,
        dimensions: { width: 220, depth: 95, height: 80, unit: "cm" },
      },
    },
    response: {
      success: true,
      message: "Product created successfully",
    },
  },
  assets: {
    request: {
      method: "POST",
      path: "/api/assets/products/10/upload-session",
      body: {
        assetType: "model",
        fileName: "sofa.glb",
        mimeType: "application/octet-stream",
        fileSize: 5242880,
        metadata: {
          format: "glb",
        },
      },
    },
    response: {
      success: true,
      message: "Upload session created successfully",
      data: {
        assetType: "model",
        s3Key: "products/10/models/generated-id.glb",
        uploadUrl: "https://bucket.s3.amazonaws.com/...",
        uploadMethod: "PUT",
        expiresAt: "2026-04-04T12:00:00.000Z",
        uploadToken: "signed-session-token",
      },
    },
  },
  assetsTextureExamples: {
    uploadSession: {
      request: {
        method: "POST",
        path: "/api/assets/products/10/upload-session",
        body: {
          assetType: "texture",
          fileName: "wall_texture.jpg",
          mimeType: "image/jpeg",
          fileSize: 123456,
          metadata: { width: 1024, height: 1024 },
        },
      },
      response: {
        success: true,
        message: "Upload session created successfully",
        data: {
          assetType: "texture",
          s3Key: "products/10/textures/generated-id.jpg",
          uploadUrl: "https://bucket.s3.amazonaws.com/...",
          uploadMethod: "PUT",
          expiresAt: "2026-04-04T12:00:00.000Z",
          uploadToken: "signed-session-token",
        },
      },
    },
    directUpload: {
      request: {
        method: "POST",
        path: "/api/assets/products/10",
        body: {
          assetType: "texture",
          fileName: "wall_texture.jpg",
          mimeType: "image/jpeg",
          fileSize: 123456,
          metadata: { width: 1024, height: 1024 },
          fileContentBase64: "<base64-encoded-file>",
        },
      },
      response: {
        success: true,
        message: "Asset uploaded successfully",
        data: {
          id: 123,
          productId: 10,
          assetType: "texture",
          s3Key: "products/10/textures/generated-id.jpg",
          s3Url: "https://bucket.s3.amazonaws.com/..",
          fileName: "wall_texture.jpg",
          fileSize: 123456,
          mimeType: "image/jpeg",
          metadata: { width: 1024, height: 1024 },
          uploadedAt: "2026-05-03T12:00:00.000Z",
          accessUrl: "https://signed-s3-url.example/..",
        },
      },
    },
    confirm: {
      request: {
        method: "POST",
        path: "/api/assets/products/10/confirm",
        body: {
          assetType: "texture",
          fileName: "wall_texture.jpg",
          mimeType: "image/jpeg",
          fileSize: 123456,
          metadata: { width: 1024, height: 1024 },
          s3Key: "products/10/textures/generated-id.jpg",
          uploadToken: "signed-session-token",
          expiresAt: "2026-04-04T12:00:00.000Z",
        },
      },
      response: {
        success: true,
        message: "Asset registered successfully",
        data: { id: 123, assetType: "texture" },
      },
    },
  },
  assetsGroupedResponse: {
    request: {
      method: "GET",
      path: "/api/assets/products/10",
    },
    response: {
      success: true,
      message: "Product assets fetched successfully",
      data: {
        models: [
          {
            id: 11,
            fileName: "sofa.glb",
            accessUrl: "https://signed-s3-url.example/model.glb",
          },
        ],
        textures: [
          {
            id: 123,
            fileName: "wall_texture.jpg",
            accessUrl: "https://signed-s3-url.example/texture.jpg",
          },
        ],
        images: [
          {
            id: 21,
            fileName: "gallery1.jpg",
            accessUrl: "https://signed-s3-url.example/gallery1.jpg",
          },
        ],
        thumbnails: [
          {
            id: 31,
            fileName: "thumb.jpg",
            accessUrl: "https://signed-s3-url.example/thumb.jpg",
          },
        ],
      },
    },
  },
  marketplace: {
    request: {
      method: "GET",
      path: "/api/marketplace/products?search=sofa&categoryId=1&brandName=Urban%20Living%20Studio",
    },
    response: {
      success: true,
      message: "Marketplace products fetched successfully",
      data: {
        page: 1,
        limit: 10,
        total: 1,
        items: [
          {
            id: 10,
            name: "Leather Chesterfield Sofa",
            brandName: "Urban Living Studio",
            description: "Classic deep-button leather sofa",
            price: 363737,
            dimensions: { width: 220, depth: 95, height: 80, unit: "cm" },
            categoryId: 1,
            categoryName: "Living Room",
            vendorName: "Urban Living Studio",
            thumbnailUrl: "https://signed-s3-url.example/thumb.jpg",
            modelsCount: 1,
            texturesCount: 2,
          },
        ],
      },
    },
  },
  marketplaceBrands: {
    request: {
      method: "GET",
      path: "/api/marketplace/brands",
    },
    response: {
      success: true,
      message: "Marketplace brands fetched successfully",
      data: ["RenovAR Curated", "Urban Living Studio"],
    },
  },
  ar: {
    request: {
      method: "GET",
      path: "/api/ar/products/10",
    },
    response: {
      success: true,
      message: "AR-ready product fetched successfully",
      data: {
        id: 10,
        name: "Leather Chesterfield Sofa",
        price: 363737,
        currency: "PKR",
        currencySymbol: "Rs",
        modelUrl: "https://signed-s3-url.example/model.glb",
        thumbnailUrl: "https://signed-s3-url.example/thumb.jpg",
        arPlacement: {
          anchorType: "horizontal",
          allowedPlaneClassifications: ["Floor"],
          fallbackPlaneClassification: "Floor",
        },
      },
    },
  },
  templates: {
    request: {
      method: "POST",
      path: "/api/templates",
      body: {
        name: "Modern Bedroom",
        roomType: "bedroom",
        roomLayout: { walls: [], floor: {} },
        placedObjects: [],
      },
    },
    response: {
      success: true,
      message: "Template created successfully",
    },
  },
  reviews: {
    request: {
      method: "POST",
      path: "/api/reviews",
      body: {
        productId: 10,
        rating: 5,
        comment: "Looks great in AR and matches the listing.",
      },
    },
    response: {
      success: true,
      message: "Review created successfully",
    },
  },
  reports: {
    request: {
      method: "POST",
      path: "/api/reports",
      body: {
        contentType: "product",
        contentId: 10,
        reason: "This listing appears misleading.",
      },
    },
    response: {
      success: true,
      message: "Report submitted successfully",
    },
  },
  admin: {
    request: {
      method: "POST",
      path: "/api/admin/products/10/approve",
      body: {
        notes: "Approved after policy review.",
      },
    },
    response: {
      success: true,
      message: "Product approved successfully",
    },
  },
};

module.exports = apiExamples;
