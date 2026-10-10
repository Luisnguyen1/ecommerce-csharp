using ECommerce.Core.Entities;
using ECommerce.Core.Models;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Data.Json;

/// <summary>Populates a fresh database.json with a realistic demo catalog, users, coupons, reviews and orders.</summary>
public class DemoDataSeeder(string webRoot)
{
    private readonly PasswordHasher<AppUser> _hasher = new();

    public Task SeedAsync(DatabaseFile db) => Task.Run(async () =>
    {
        // Fallback image for products added without a photo (e.g. via the admin area).
        SvgImage.EnsurePlaceholder(webRoot);

        SeedCategories(db);
        SeedProducts(db);
        SeedUsers(db);
        SeedCoupons(db);
        SeedReviews(db);
        await SeedOrdersAsync(db);
    });

    private static void SeedCategories(DatabaseFile db)
    {
        db.Categories.AddRange(
            new Category { Name = "Audio", Slug = "audio", Icon = "🎧", DisplayOrder = 1, Description = "Headphones, earbuds and speakers for every budget." },
            new Category { Name = "Wearables", Slug = "wearables", Icon = "⌚", DisplayOrder = 2, Description = "Smartwatches and fitness trackers." },
            new Category { Name = "Smart Home", Slug = "smart-home", Icon = "🏠", DisplayOrder = 3, Description = "Lights, hubs and sensors that make home smarter." },
            new Category { Name = "Gaming", Slug = "gaming", Icon = "🎮", DisplayOrder = 4, Description = "Controllers, keyboards and mice built for play." },
            new Category { Name = "Cameras", Slug = "cameras", Icon = "📷", DisplayOrder = 5, Description = "Action cams, drones and gear for creators." },
            new Category { Name = "Accessories", Slug = "accessories", Icon = "🎒", DisplayOrder = 6, Description = "Bags, cables, stands and daily essentials." });
    }

    private void SeedProducts(DatabaseFile db)
    {
        var cat = db.Categories.ToDictionary(c => c.Slug, c => c.Id);
        var day = 0;
        var created = () => DateTime.UtcNow.AddDays(-(day += 2));

        // Photos live in wwwroot/images/catalog/{slug}-{1,2}.jpg — real product photography,
        // see ATTRIBUTIONS.md at the repo root for per-image credits and licenses.
        void Add(string categorySlug, string name, string brand, string description, decimal price,
            decimal? compareAt = null, string? badge = null, bool featured = false,
            (string name, string? color, string? size, decimal? priceOverride, int stock)[]? variants = null)
        {
            var slug = name.ToLowerInvariant().Replace(" ", "-").Replace("--", "-");
            var product = new Product
            {
                Name = name,
                Slug = slug,
                Sku = $"{brand.ToUpperInvariant()[..3]}-{slug[..Math.Min(slug.Length, 8)].ToUpperInvariant()}",
                Brand = brand,
                Description = description,
                CategoryId = cat[categorySlug],
                Price = price,
                CompareAtPrice = compareAt,
                Badge = badge,
                IsFeatured = featured,
                CreatedAt = created(),
                Images = [$"/images/catalog/{slug}-1.jpg", $"/images/catalog/{slug}-2.jpg"]
            };

            if (variants is { Length: > 0 })
            {
                foreach (var v in variants)
                {
                    product.Variants.Add(new ProductVariant
                    {
                        Name = v.name,
                        Color = v.color,
                        Size = v.size,
                        Sku = $"{product.Sku}-{v.name.ToUpperInvariant().Replace(" ", "").Replace("/", "")}",
                        PriceOverride = v.priceOverride,
                        Stock = v.stock
                    });
                }
            }
            else
            {
                product.Variants.Add(new ProductVariant { Name = "Standard", Sku = product.Sku + "-STD", Stock = Random.Shared.Next(20, 90) });
            }

            db.Products.Add(product);
        }

        // Audio
        Add("audio", "Bose QuietComfort Headphones", "Bose", "Legendary noise cancelling with aware mode, 24 hours of battery and a lightweight fold-flat design made for travel.", 329.00m, 379.00m, "Sale", true,
            [("Triple Black", "Black", null, null, 21), ("Smoke White", "White", null, null, 14), ("Midnight Blue", "Blue", null, null, 8)]);
        Add("audio", "Apple AirPods Pro 2", "Apple", "Adaptive audio that tunes to your surroundings, active noise cancellation and a MagSafe charging case with built-in speaker.", 249.00m, null, "New", true,
            [("White", "White", null, null, 45)]);
        Add("audio", "JBL Flip 6 Speaker", "JBL", "Bold JBL Pro Sound in a rugged IP67 body with 12 hours of playtime and PartyBoost pairing for twice the sound.", 99.95m, 129.95m, "Sale", false,
            [("Black", "Black", null, null, 20), ("Blue", "Blue", null, null, 15), ("Green", "Green", null, null, 9)]);
        Add("audio", "Audioengine A2 Plus Speakers", "Audioengine", "Premium desktop speakers with a built-in DAC and Bluetooth — aramid-fiber woofers and silk-dome tweeters that fill a room.", 269.00m, null, null, false,
            [("Black", "Black", null, null, 11), ("White", "White", null, null, 7), ("Red", "Red", null, null, 4)]);
        Add("audio", "Blue Yeti USB Microphone", "Blue", "The studio-standard USB mic with four pickup patterns, zero-latency headphone monitoring and gain control right on the mic.", 129.99m, null, null, false,
            [("Blackout", "Black", null, null, 30), ("Silver", "Silver", null, null, 22), ("Midnight Blue", "Blue", null, null, 10)]);

        // Wearables
        Add("wearables", "Apple Watch Ultra 2", "Apple", "Rugged 49mm titanium case, the brightest Always-On display Apple has made and dual-frequency GPS with up to 36 hours of battery.", 799.00m, null, "Hot", true,
            [("49mm Blue Trail Loop", "Blue", "49mm", null, 6), ("49mm Green Trail Loop", "Green", "49mm", null, 4), ("49mm Orange Trail Loop", "Orange", "49mm", null, 5)]);
        Add("wearables", "Fitbit Charge 6", "Fitbit", "Daily readiness, ECG, SpO2 and built-in GPS in a slim band that goes two weeks between charges.", 159.95m, null, null, false,
            [("Black", "Black", null, null, 40), ("Coral", "Coral", null, null, 25), ("White", "White", null, null, 18)]);
        Add("wearables", "Samsung Galaxy Watch Active2", "Samsung", "Slim aluminum smartwatch with sleep coaching, ECG and continuous heart rate monitoring — Galaxy and Android friendly.", 199.99m, 269.99m, "Sale", false,
            [("40mm Black", "Black", "40mm", null, 12), ("44mm Black", "Black", "44mm", null, 8), ("44mm Silver", "Silver", "44mm", null, 6)]);
        Add("wearables", "Garmin Vivoactive Smartwatch", "Garmin", "GPS sports watch with activity tracking, heart rate and smart notifications in a slim everyday design.", 199.99m, 249.99m, "Sale", false,
            [("Black", "Black", null, null, 14), ("White", "White", null, null, 5)]);

        // Smart Home
        Add("smart-home", "Philips Hue Color Bulb 4-Pack", "Philips", "16 million colors plus warm-to-cool white, with scenes, schedules and automations in the Hue app. Works with Matter.", 179.99m, 199.99m, "Sale", true,
            [("4-Pack", null, null, null, 35)]);
        Add("smart-home", "Arlo Essential Video Doorbell", "Arlo", "Wire-free 2K HDR doorbell with person detection, two-way audio and a 180° head-to-toe field of view. No hub needed.", 129.99m, 179.99m, "Hot", true,
            [("White", "White", null, null, 16), ("Black", "Black", null, null, 7)]);
        Add("smart-home", "Levoit Core 300S Air Purifier", "Levoit", "Compact HEPA purifier for bedrooms and offices, with a quiet sleep mode and real-time air quality feedback in the app.", 129.99m, 149.99m, "Sale", false,
            [("White", "White", null, null, 18)]);
        Add("smart-home", "Amazon Echo Dot 5th Gen", "Amazon", "The fifth-generation Dot with clearer sound, on-device Alexa, a temperature sensor and built-in eero hub.", 49.99m, 59.99m, "Sale", true,
            [("Charcoal", "Black", null, null, 60), ("Glacier White", "White", null, null, 45), ("Deep Sea Blue", "Blue", null, null, 20)]);
        Add("smart-home", "Google Nest Hub 2nd Gen", "Google", "7-inch smart display with sleep sensing, voice control for your connected home and a digital photo frame.", 89.99m, 99.99m, "Sale", false,
            [("Chalk", "White", null, null, 22), ("Charcoal", "Black", null, null, 19)]);

        // Gaming
        Add("gaming", "Sony DualSense Controller", "Sony", "Haptic feedback and adaptive triggers over Bluetooth for PS5 and PC, with a built-in mic and headset jack.", 74.99m, null, "Hot", true,
            [("White", "White", null, null, 38), ("Midnight Black", "Black", null, null, 24), ("Cosmic Red", "Red", null, null, 12)]);
        Add("gaming", "Razer BlackWidow Keyboard", "Razer", "Mechanical gaming keyboard with tactile Razer Green switches, per-key green backlighting and a magnetic wrist rest.", 139.99m, 169.99m, "Sale", false,
            [("Razer Green", "Green", null, null, 15), ("Razer Orange", "Orange", null, null, 9)]);
        Add("gaming", "Logitech G502 HERO Mouse", "Logitech", "HERO 25K sensor, 11 programmable buttons and a tunable weight system in one of the most loved gaming mice ever made.", 49.99m, 69.99m, "Sale", false,
            [("Standard", null, null, null, 50)]);
        Add("gaming", "SteelSeries Arctis Nova 7", "SteelSeries", "Wireless headset with the Nova acoustic system, simultaneous 2.4GHz + Bluetooth and a 38-hour battery.", 179.99m, null, null, false,
            [("Black", "Black", null, null, 17), ("White", "White", null, null, 9)]);
        Add("gaming", "Elgato Stream Deck MK2", "Elgato", "15 customizable LCD keys that trigger actions, scenes and macros in your favorite apps, streams and workflows.", 149.99m, null, "New", false,
            [("Standard", null, null, null, 20)]);

        // Cameras
        Add("cameras", "GoPro HERO12 Black", "GoPro", "5.3K60 video, HDR photos and HyperSmooth 6.0 stabilization in a waterproof 154g body that mounts anywhere.", 299.00m, 349.00m, "Sale", true,
            [("Standard", null, null, null, 14), ("Creator Edition", null, null, 449.00m, 5)]);
        Add("cameras", "DJI Mavic Mini Drone", "DJI", "Weighs under 249 g with a 3-axis gimbal, 2.7K camera and 30 minutes of flight time — no registration in most regions.", 399.00m, null, "Hot", false,
            [("Standard", null, null, null, 8), ("Fly More Combo", null, null, 499.00m, 4)]);
        Add("cameras", "DJI Osmo Pocket", "DJI", "Pocket-sized 3-axis stabilized 4K camera with active tracking, face tracking and a built-in display.", 349.00m, null, null, false,
            [("Standard", null, null, null, 10)]);
        Add("cameras", "Neewer 18 Inch Ring Light Kit", "Neewer", "Bi-color 18-inch ring light with stand, phone holder and filters for streaming, makeup and video calls.", 99.99m, null, null, false,
            [("Standard", null, null, null, 26)]);

        // Accessories
        Add("accessories", "Herschel Heritage Backpack", "Herschel", "Signature style with a padded 15-inch laptop sleeve, media pocket and contoured shoulder straps for daily carry.", 99.99m, 109.99m, "Sale", true,
            [("Blue", "Blue", null, null, 18), ("Olive", "Green", null, null, 14), ("Black", "Black", null, null, 20)]);
        Add("accessories", "Logitech MX Master 3S", "Logitech", "Flagship productivity mouse with an 8K DPI sensor, quiet clicks and MagSpeed scrolling on any surface.", 99.99m, null, null, true,
            [("Graphite", "Black", null, null, 28), ("Pale Gray", "Gray", null, null, 12)]);
        Add("accessories", "Anker PowerLine USB-C Cable", "Anker", "Braided USB-C to USB-A cable with 480 Mbps sync and a 5,000-bend lifespan tested for toughness.", 15.99m, null, null, false,
            [("White", "White", null, null, 80), ("Red", "Red", null, null, 55), ("Blue", "Blue", null, null, 48)]);
        Add("accessories", "Rain Design mStand Laptop Stand", "Rain Design", "Solid aluminum stand that raises your screen six inches, improves airflow and matches your MacBook.", 55.99m, 69.99m, "Sale", false,
            [("Silver", "Silver", null, null, 24), ("Space Gray", "Gray", null, null, 20)]);

        // A few low-stock items to make the admin dashboard interesting.
        foreach (var slug in new[] { "apple-watch-ultra-2", "dji-mavic-mini-drone" })
        {
            var p = db.Products.First(x => x.Slug == slug);
            foreach (var v in p.Variants) v.Stock = Math.Min(v.Stock, 4);
        }
    }

    private void SeedUsers(DatabaseFile db)
    {
        AppUser NewUser(string email, string name, string role, string password)
        {
            var u = new AppUser { Email = email.ToLowerInvariant(), FullName = name, Role = role };
            u.PasswordHash = _hasher.HashPassword(u, password);
            return u;
        }

        var admin = NewUser("admin@nimbus.dev", "Alex Admin", Roles.Admin, "Admin123!");
        var demo = NewUser("demo@nimbus.dev", "Dana Demo", Roles.Customer, "Demo1234!");
        var maya = NewUser("maya@example.com", "Maya Chen", Roles.Customer, "Maya1234!");
        var liam = NewUser("liam@example.com", "Liam Novak", Roles.Customer, "Liam1234!");
        var sofia = NewUser("sofia@example.com", "Sofia Reyes", Roles.Customer, "Sofia1234!");

        demo.SavedAddresses.Add(new Address
        {
            FullName = "Dana Demo", Email = "demo@nimbus.dev", Phone = "+1 555 010 1234",
            Line1 = "742 Evergreen Terrace", City = "Springfield", State = "IL",
            PostalCode = "62704", Country = "United States"
        });

        db.Users.AddRange(admin, demo, maya, liam, sofia);
    }

    private static void SeedCoupons(DatabaseFile db)
    {
        db.Coupons.AddRange(
            new Coupon
            {
                Code = "WELCOME10",
                Description = "10% off your first order",
                DiscountType = DiscountType.Percent, Value = 10,
                ExpiresAt = DateTime.UtcNow.AddMonths(6)
            },
            new Coupon
            {
                Code = "SAVE20",
                Description = "20% off orders over $100",
                DiscountType = DiscountType.Percent, Value = 20,
                MinOrderAmount = 100,
                ExpiresAt = DateTime.UtcNow.AddMonths(2)
            },
            new Coupon
            {
                Code = "NIMBUS5",
                Description = "$5 off orders over $30",
                DiscountType = DiscountType.Fixed, Value = 5,
                MinOrderAmount = 30,
                UsageLimit = 100
            },
            new Coupon
            {
                Code = "EXPIRED1",
                Description = "An expired coupon for demo purposes",
                DiscountType = DiscountType.Percent, Value = 15,
                ExpiresAt = DateTime.UtcNow.AddDays(-3)
            });
    }

    private void SeedReviews(DatabaseFile db)
    {
        var bySlug = db.Products.ToDictionary(p => p.Slug, p => p.Id);
        var (maya, liam, sofia, demo) = (db.Users[2], db.Users[3], db.Users[4], db.Users[1]);

        void Add(string slug, AppUser user, int rating, string title, string body, int daysAgo)
        {
            db.Reviews.Add(new Review
            {
                ProductId = bySlug[slug], UserId = user.Id, UserName = user.FullName,
                Rating = rating, Title = title, Body = body,
                CreatedAt = DateTime.UtcNow.AddDays(-daysAgo)
            });
        }

        Add("bose-quietcomfort-headphones", maya, 5, "Silence on demand", "The noise cancelling makes my open office disappear. Battery gets me through a full week of commuting without a charge.", 21);
        Add("bose-quietcomfort-headphones", liam, 4, "Superb for travel", "Wore them on a ten-hour flight and forgot I had them on. Aware mode is great for announcements.", 12);
        Add("apple-airpods-pro-2", sofia, 5, "Tiny but mighty", "Adaptive audio just works — it quiets the streetcar without me touching anything. The case speaker is a nice touch.", 5);
        Add("apple-watch-ultra-2", maya, 5, "Two days of hiking on one charge", "GPS track matched my phone's exactly, and the display is readable in direct alpine sun. Heavy, but that's the point.", 25);
        Add("apple-watch-ultra-2", sofia, 4, "Bright, bold, big", "The Action button is addictive. Wish the bands were easier to swap with cold fingers.", 14);
        Add("garmin-vivoactive-smartwatch", sofia, 4, "Simple, accurate GPS", "Light on the wrist and the battery genuinely lasts a week. Tracks my runs without any fuss.", 2);
        Add("fitbit-charge-6", sofia, 4, "Does 90% of a smartwatch", "Sleep and readiness scores are what I actually care about. App subscription upsells get old, though.", 18);
        Add("philips-hue-color-bulb-4-pack", demo, 4, "Setup took two minutes", "Matter support means they showed up in my existing app instantly. Movie night scene is the family favorite.", 30);
        Add("amazon-echo-dot-5th-gen", maya, 4, "Alexa in every room", "Sounds far better than a dot this size should. The temperature sensor actually drives my fan automation.", 10);
        Add("sony-dualsense-controller", liam, 5, "Haptics are a game changer", "Feeling rain patter and engine rumble separately never gets old. Battery easily lasts a long weekend of play.", 16);
        Add("razer-blackwidow-keyboard", maya, 5, "That classic click", "Green switches feel crisp and the backlight looks great in a dark room. Wrist rest is genuinely useful.", 8);
        Add("gopro-hero12-black", sofia, 5, "Took it skiing", "Horizon lock is witchcraft — footage looks gimbal-stabilized. Battery is decent in the cold, too.", 11);
        Add("herschel-heritage-backpack", demo, 5, "Perfect everyday bag", "Fits my 15-inch laptop, lunch and a hoodie. The blue canvas gets compliments constantly.", 6);
        Add("logitech-mx-master-3s", liam, 5, "The best mouse for work", "MagSpeed scrolling flies through spreadsheets and the thumb wheel is perfect for timeline scrubs.", 9);
        Add("jbl-flip-6-speaker", liam, 4, "Room-filling sound", "Bass is deeper than expected for the size. Took it to the beach and shrugged off sand and splashes.", 3);
        Add("dji-osmo-pocket", demo, 5, "Gimbal in your pocket", "Walking footage looks like it was shot on a rig ten times the size. Charges over the same cable as my phone.", 20);
    }

    private async Task SeedOrdersAsync(DatabaseFile db)
    {
        var demo = db.Users[1];
        var maya = db.Users[2];
        var liam = db.Users[3];
        var sofia = db.Users[4];
        var bySlug = db.Products.ToDictionary(p => p.Slug, p => p);

        async Task AddOrder(AppUser user, OrderStatus status, int daysAgo, decimal shippingFee,
            (string slug, string variant, int qty)[] lines, string? coupon = null)
        {
            var order = new Order
            {
                OrderNumber = $"ORD-{DateTime.UtcNow.AddDays(-daysAgo):yyyyMMdd}-{Random.Shared.Next(1000, 10000)}",
                UserId = user.Id,
                Email = user.Email,
                Status = status,
                PlacedAt = DateTime.UtcNow.AddDays(-daysAgo),
                UpdatedAt = status == OrderStatus.Paid ? null : DateTime.UtcNow.AddDays(-daysAgo + 1),
                ShippingAddress = new Address
                {
                    FullName = user.FullName, Email = user.Email, Phone = "+1 555 010 0000",
                    Line1 = "100 Demo Street", City = "Portland", State = "OR", PostalCode = "97201"
                },
                ShippingMethodId = shippingFee > 0 ? Shipping.ExpressId : Shipping.StandardId,
                ShippingMethodName = shippingFee > 0 ? "Express Shipping" : "Standard Shipping",
                CouponCode = coupon,
                PaymentTransactionId = $"txn_{Guid.NewGuid():N}",
                CardLast4 = "4242"
            };

            foreach (var (slug, variant, qty) in lines)
            {
                var product = bySlug[slug];
                var v = product.Variants.First(x => x.Name == variant);
                order.Lines.Add(new OrderLine
                {
                    ProductId = product.Id, ProductName = product.Name, VariantName = v.Name,
                    Sku = v.Sku, ImageUrl = product.Images[0],
                    UnitPrice = v.EffectivePrice(product.Price), Quantity = qty
                });
            }

            order.Subtotal = order.Lines.Sum(l => l.LineTotal);
            order.Discount = coupon == "WELCOME10" ? Math.Round(order.Subtotal * 0.10m, 2) : 0;
            order.ShippingFee = shippingFee;
            order.Total = order.Subtotal - order.Discount + order.ShippingFee;

            // Keep the demo inventory honest.
            foreach (var (slug, variant, qty) in lines)
            {
                var v = bySlug[slug].Variants.First(x => x.Name == variant);
                v.Stock = Math.Max(0, v.Stock - Math.Min(qty, 2));
            }

            db.Orders.Add(order);
            if (status == OrderStatus.Paid)
            {
                db.Emails.Add(new EmailMessage
                {
                    To = user.Email, Type = EmailType.OrderConfirmation,
                    Subject = $"Order {order.OrderNumber} confirmed 🎉",
                    Body = $"Hi {user.FullName},\n\nThanks for your order! We received your payment and your order is now being prepared.\n\nOrder: {order.OrderNumber}\nItems: {order.Lines.Sum(l => l.Quantity)}\nTotal: {order.Total:C}\n\nWe'll email you again when it ships.\n\n— The Nimbus Team",
                    SentAt = order.PlacedAt
                });
            }
            await Task.CompletedTask;
        }

        await AddOrder(maya, OrderStatus.Delivered, 28, 0, [("bose-quietcomfort-headphones", "Triple Black", 1)], "WELCOME10");
        await AddOrder(liam, OrderStatus.Delivered, 24, Shipping.ExpressFee, [("sony-dualsense-controller", "White", 1), ("logitech-g502-hero-mouse", "Standard", 1)]);
        await AddOrder(maya, OrderStatus.Shipped, 9, 0, [("razer-blackwidow-keyboard", "Razer Green", 1)]);
        await AddOrder(sofia, OrderStatus.Shipped, 7, Shipping.ExpressFee, [("gopro-hero12-black", "Creator Edition", 1)]);
        await AddOrder(demo, OrderStatus.Paid, 4, 0, [("herschel-heritage-backpack", "Blue", 1), ("anker-powerline-usb-c-cable", "White", 2)]);
        await AddOrder(liam, OrderStatus.Paid, 2, 0, [("apple-airpods-pro-2", "White", 1), ("philips-hue-color-bulb-4-pack", "4-Pack", 1)], "NIMBUS5");
        await AddOrder(sofia, OrderStatus.Cancelled, 15, Shipping.StandardFee, [("apple-watch-ultra-2", "49mm Blue Trail Loop", 1)]);
    }
}
