const titleCase = (value) => String(value || "").trim();

const PRODUCT_COPY = {
  "Basic Wooden Shelf":
    "A compact wooden shelf for organizing books, decor, and everyday essentials. Its simple profile makes it easy to preview on walls or beside furniture without overwhelming the room.",
  "Ceiling Lights":
    "A ceiling-mounted light fixture for previewing overhead lighting placement in bedrooms, lounges, and dining areas. Use it to judge scale, height, and visual balance before installation.",
  "Foot Stool":
    "A small foot stool that works as extra seating, a leg rest, or a casual accent piece. Its compact size is useful for testing tight corners and seating arrangements in AR.",
  "Ladder Bookshelf":
    "A leaning ladder-style bookshelf for displaying books, plants, and decorative objects. The open vertical frame adds storage while keeping the room visually light.",
  "Long Drawer With Cabinet And Shelf":
    "A long storage unit with drawers, cabinet space, and open shelving for media rooms, bedrooms, or entry areas. It helps users preview both storage capacity and wall-length fit.",
  "Metal Shoe Rack":
    "A practical metal shoe rack for entryways, closets, and utility spaces. The slim structure is useful for checking walkway clearance and everyday storage placement.",
  "Pothos Potted Money Plant":
    "A potted pothos plant for adding greenery to shelves, tables, and room corners. It gives users an easy way to test soft decorative accents in the space.",
  "Wooden Bar Cabinet":
    "A wooden bar cabinet for storing glassware, bottles, and entertaining essentials. Its cabinet form is suitable for dining rooms, lounges, and feature corners.",
  "Wooden Bookshelf":
    "A wooden bookshelf for books, decor, and display storage. The classic form helps users judge height, depth, and visual weight against nearby furniture.",
  "Wooden China Cabinet":
    "A display cabinet for dinnerware, glassware, and decorative collections. It is suited to dining rooms and formal living spaces where vertical storage is needed.",
  "Wooden Pendant Light":
    "A wooden pendant light for previewing warm overhead lighting above tables, counters, or seating areas. Use it to test drop height and focal placement in AR.",
  "Wooden Shelf":
    "A versatile wooden shelf for displaying decor, books, or small storage items. Its clean shape makes it easy to place in living rooms, bedrooms, and workspaces.",
  "Wooden Storage Cabinet":
    "A wooden storage cabinet for keeping household items concealed and organized. It works well in living rooms, bedrooms, hallways, and multipurpose spaces.",
  "Bunk Bed":
    "A stacked bunk bed designed for shared bedrooms and compact sleeping layouts. Preview it in AR to check ceiling clearance, ladder access, and floor space.",
  "Classic Wooden Bed":
    "A classic wooden bed frame with a warm, timeless look for bedroom layouts. It helps users judge bed scale, side clearance, and furniture alignment.",
  "Eco Brand Bed":
    "A simple bed frame option for planning clean, practical bedroom layouts. Use it to preview the footprint, headboard position, and surrounding circulation.",
  "Faydra Full Bed":
    "A full-size bed design for comfortable bedroom planning. Its balanced proportions make it useful for testing placement against wardrobes, side tables, and windows.",
  "Medieval Bed":
    "A characterful bed frame with a traditional, statement-style presence. It is best for previewing dramatic bedroom focal points and larger room arrangements.",
  "Modern Bed 01":
    "A modern bed frame with a clean silhouette for contemporary bedrooms. Preview it to check fit, walking space, and coordination with nearby storage.",
  "Modern Bed 02":
    "A contemporary bed frame for streamlined bedroom layouts. Its simple form helps users evaluate scale and placement without visual clutter.",
  "Storage Platform Bed":
    "A platform bed with integrated storage appeal for space-conscious bedrooms. Use AR placement to check both sleeping area and storage access around the frame.",
  "Tree Canopy Bed":
    "A canopy-style bed that creates a strong bedroom focal point. Preview it to confirm ceiling clearance, room proportion, and the visual impact of the frame.",
  "Upholstered Bed":
    "An upholstered bed frame with a softer, more comfortable bedroom look. It is useful for comparing fabric-style finishes and headboard scale in AR.",
  "Blue Lounge Chair":
    "A lounge chair with a blue accent finish for adding color to living rooms, bedrooms, or reading corners. Preview it to test comfort zones and color balance.",
  "Classic Tufted Armchair":
    "A tufted armchair with a traditional, cushioned look for lounges and bedrooms. It works well as a statement chair or paired seating accent.",
  "Indochine Upholstered Arm Chair":
    "An upholstered arm chair with an elegant accent profile for living rooms, bedrooms, and reading areas. Use it to preview style, scale, and corner placement.",
  "Leather Dining Chair":
    "A leather dining chair for dining rooms, desks, or occasional seating. AR preview helps confirm table clearance, chair spacing, and material contrast.",
  "Lounge Accent Chair":
    "An accent lounge chair for creating a comfortable reading spot or conversational seating area. It is useful for testing room flow around sofas and tables.",
  "Modern Upholstered Armchair 01":
    "A modern upholstered armchair for clean, comfortable living room seating. Preview it beside sofas, side tables, or windows to compare scale and spacing.",
  "Modern Upholstered Armchair 02":
    "A contemporary upholstered armchair with a soft, structured presence. It helps users plan lounge seating without guessing the chair footprint.",
  "Modern Upholstered Lounge Chair":
    "A modern lounge chair for relaxed seating areas, bedrooms, and quiet corners. Use it to test comfort placement and visual balance with nearby furniture.",
  "Office Chair":
    "An office chair for workspace planning, desk pairing, and home office layouts. AR preview helps users confirm chair clearance and movement space.",
  "Rocking Chair":
    "A rocking chair for nurseries, bedrooms, and relaxed living spaces. Preview it with extra clearance so the rocking motion has comfortable room.",
  "Vintage Arm Chair":
    "A vintage-inspired arm chair for adding character to living rooms, bedrooms, or reading corners. Its classic shape works well as a standalone accent.",
  "Vintage Bar Stool":
    "A bar stool for counters, breakfast bars, and tall tables. Use AR to check seat height, spacing, and clearance before choosing multiples.",
  "Wooden Arm Chair":
    "A wooden arm chair for dining, lounge, or accent seating. Its solid frame helps users compare natural material tones and chair proportions.",
  "Wooden Chair":
    "A simple wooden chair for dining, desk, or occasional seating layouts. Preview it to confirm scale, spacing, and coordination with surrounding furniture.",
  "L Shaped Sofa":
    "An L-shaped sofa for anchoring living rooms and family seating areas. AR preview helps confirm chaise direction, walking paths, and room proportion.",
  "Leather Armchair":
    "A leather armchair for comfortable solo seating in lounges, offices, or bedrooms. It adds a grounded accent while keeping placement easy to evaluate.",
  "Leather Chesterfield Sofa":
    "A leather Chesterfield sofa with deep-buttoned classic styling for formal lounges and statement seating areas. Use AR to compare its scale and visual weight.",
  "Modern Accent Armchair":
    "A modern accent armchair for adding extra seating and style to living rooms or bedrooms. It is ideal for testing color, footprint, and corner placement.",
  "Sofa With Pillows":
    "A sofa with pillows for comfortable living room seating and a finished, styled look. Preview it to evaluate width, depth, and coordination with tables.",
  "Three Sitter Sofa":
    "A three-seater sofa for everyday living room seating. AR placement helps users check wall fit, table distance, and circulation around the sofa.",
  "Tufted Living Sofa":
    "A tufted living room sofa with a tailored, cushioned appearance. It works well for comparing classic comfort styling against modern room layouts.",
  "Antique Wooden Table":
    "An antique-style wooden table with a warm, traditional presence. Use it in AR to judge room proportion, seating space, and wood tone compatibility.",
  "Farmhouse Wooden Table":
    "A farmhouse wooden table for dining rooms and casual gathering spaces. Its rustic character helps users test a warmer, more relaxed room style.",
  "Glass Table":
    "A glass table for dining or living spaces where a lighter visual footprint is preferred. Preview it to compare transparency, size, and surrounding clearance.",
  "Mid Century Modern Coffee Table":
    "A mid-century modern coffee table for living rooms with clean lines and practical surface space. Use AR to check sofa distance and walking clearance.",
  "Round Side Table":
    "A round side table for beside sofas, beds, or accent chairs. Its compact shape is useful for filling small gaps without blocking movement.",
  "Semi Round Wooden Table":
    "A semi-round wooden table for compact dining, side-wall placement, or accent use. Preview it to test curved edges and space-saving layouts.",
  "Wooden Dining Table":
    "A wooden dining table for family meals and everyday gathering spaces. AR placement helps confirm seating room, table scale, and movement around the dining area.",
};

const WALL_TEXTURE_COPY = {
  "Ceramic Glazed":
    "A glossy ceramic wall texture with a handcrafted glazed finish. It is suited to feature walls, backsplashes, and decorative accent surfaces in AR previews.",
  "Concrete Worn":
    "A worn concrete wall texture with subtle weathering and industrial character. Use it to preview raw, modern, or urban wall finishes.",
  "Rammed Earth":
    "A layered rammed earth wall texture with natural striations and a grounded mineral look. It is ideal for warm, organic feature walls.",
  "Rusty Gold Ceramic Glazed":
    "A rich ceramic wall texture with rusty gold glazing and a reflective handmade feel. It works well for bold accent walls and premium decorative surfaces.",
  "Stone Bricks":
    "A split-face stone brick wall texture with rugged depth and masonry character. Use it for feature walls, fireplace surrounds, and exterior-inspired interiors.",
  "Stone Quartzite":
    "A quartzite stone wall texture with layered natural variation. It is suited to refined feature walls, textured panels, and upscale interior surfaces.",
  "Wood Veneer Oak":
    "An oak wood veneer wall texture with natural grain and warm paneling character. It helps users preview timber-clad feature walls and cozy interior finishes.",
};

const numberedDescription = (name) => {
  if (/^Bed \d+/i.test(name)) {
    return `A bed frame option for planning bedroom layouts in AR. Use ${name} to compare footprint, headboard placement, and walking clearance around the bed.`;
  }

  if (/^Chair \d+/i.test(name)) {
    return `A chair option for testing seating layouts, spacing, and furniture scale. ${name} is useful for comparing dining, desk, or accent chair placement in AR.`;
  }

  if (/^Center Table \d+/i.test(name)) {
    return `A center table for living room seating arrangements. ${name} helps users preview table size, sofa distance, and open walking space.`;
  }

  if (/^Coffee Table \d+/i.test(name)) {
    return `A coffee table for pairing with sofas and lounge chairs. ${name} is useful for checking reach, clearance, and living room balance in AR.`;
  }

  if (/^Wooden Chair \d+/i.test(name)) {
    return `A wooden chair option for dining, desk, or accent seating. ${name} helps users compare natural material tone, scale, and spacing.`;
  }

  return null;
};

const categoryFallback = (entry) => {
  const name = titleCase(entry.name);
  const category = String(entry.sourceCategory || entry.category || "").toLowerCase();

  if (category === "beds") {
    return `${name} is a bedroom furniture piece for previewing bed placement, room scale, and surrounding clearance before purchase.`;
  }

  if (category === "chairs" || category === "sofas") {
    return `${name} is a seating piece for planning comfortable living room, bedroom, or workspace layouts in AR. Use it to compare scale, spacing, and style.`;
  }

  if (category === "tables" || category === "coffee tables") {
    return `${name} is a table option for planning dining or living room layouts. Preview it in AR to check size, clearance, and how it pairs with nearby seating.`;
  }

  return `${name} is a curated home furnishing for RenovAR AR previews, helping users understand its scale, placement, and fit inside their room.`;
};

const getProductDescription = (entry) =>
  PRODUCT_COPY[entry.name] || numberedDescription(entry.name) || categoryFallback(entry);

const getWallTextureDescription = (entry) =>
  WALL_TEXTURE_COPY[entry.name] || `${entry.name} is a wall texture for AR surface previews, designed to help users compare wall finish, color, and material character.`;

module.exports = {
  getProductDescription,
  getWallTextureDescription,
};
