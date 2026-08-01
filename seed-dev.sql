-- =============================================================================
-- Données de test — développement uniquement (NTR-81)
--
-- ⚠️ NE JAMAIS EXÉCUTER EN PRODUCTION.
--    Ces comptes fictifs fausseraient les KPIs du dashboard admin et
--    apparaîtraient dans les exports RGPD.
--
-- À exécuter APRÈS les migrations EF Core (les tables doivent exister) :
--   psql -h localhost -p 5445 -U postgres -d nutrition_dev -f seed-dev.sql
--
-- Les keycloak_id correspondent aux identifiants fixes des utilisateurs
-- déclarés dans keycloak/realm-export.json. Les deux fichiers doivent
-- rester synchronisés.
--
-- Le script est idempotent : il peut être relancé sans erreur.
-- =============================================================================

BEGIN;

-- ── Utilisateurs ─────────────────────────────────────────────────────────────
-- Le palier d'abonnement (subscription_tier) est une colonne en base, pas un
-- rôle Keycloak : c'est ici qu'il est positionné, pas dans le realm-export.

INSERT INTO users (id, keycloak_id, birth_date, gender, activity_level, height,
                   allergies, dietary_preferences, subscription_tier, created_at, deleted_at)
VALUES
    ('aaaa0000-0000-0000-0000-000000000001',
     '11111111-0000-0000-0000-000000000001',
     DATE '1990-05-14', 'Male', 'ModeratelyActive', 178,
     '{}', '{}', 'Free', NOW(), NULL),

    ('aaaa0000-0000-0000-0000-000000000002',
     '11111111-0000-0000-0000-000000000002',
     DATE '1988-11-02', 'Female', 'VeryActive', 165,
     '{Gluten}', '{vegetarien}', 'Pro', NOW(), NULL),

    ('aaaa0000-0000-0000-0000-000000000003',
     '11111111-0000-0000-0000-000000000003',
     DATE '1985-01-20', 'Other', 'Sedentary', 172,
     '{}', '{}', 'Free', NOW(), NULL)
ON CONFLICT (id) DO NOTHING;

-- ── Pesées ───────────────────────────────────────────────────────────────────
-- Au moins une pesée par utilisateur : le lancement d'une Diet en exige une
-- (DietService lève UnprocessableException sinon).

INSERT INTO weight_entries (id, user_id, weight, measured_at)
VALUES
    ('bbbb0000-0000-0000-0000-000000000001', 'aaaa0000-0000-0000-0000-000000000001', 82.5, CURRENT_DATE - 14),
    ('bbbb0000-0000-0000-0000-000000000002', 'aaaa0000-0000-0000-0000-000000000001', 81.2, CURRENT_DATE - 7),
    ('bbbb0000-0000-0000-0000-000000000003', 'aaaa0000-0000-0000-0000-000000000001', 80.4, CURRENT_DATE),
    ('bbbb0000-0000-0000-0000-000000000004', 'aaaa0000-0000-0000-0000-000000000002', 61.0, CURRENT_DATE - 7),
    ('bbbb0000-0000-0000-0000-000000000005', 'aaaa0000-0000-0000-0000-000000000002', 60.5, CURRENT_DATE),
    ('bbbb0000-0000-0000-0000-000000000006', 'aaaa0000-0000-0000-0000-000000000003', 75.0, CURRENT_DATE)
ON CONFLICT (id) DO NOTHING;

-- ── Plans diététiques personnels ─────────────────────────────────────────────
-- Les 4 templates partagés sont déjà créés par la migration SeedTemplates.

INSERT INTO diet_plans (id, user_id, name, is_template, diet_type, goal, target_weight,
                        macro_protein_pct, macro_carb_pct, macro_fat_pct)
VALUES
    ('cccc0000-0000-0000-0000-000000000001',
     'aaaa0000-0000-0000-0000-000000000001',
     'Mon plan sèche', false, 'LowCarb', 'WeightLoss', 75, 40, 30, 30),

    ('cccc0000-0000-0000-0000-000000000002',
     'aaaa0000-0000-0000-0000-000000000002',
     'Plan endurance', false, 'Mediterranean', 'Maintenance', 60, 25, 50, 25)
ON CONFLICT (id) DO NOTHING;

-- ── Aliments ─────────────────────────────────────────────────────────────────
-- Quelques aliments pour composer des repas sans attendre l'import Open Food
-- Facts (NTR-55), qui peuplera réellement cette table.

INSERT INTO food_items (id, off_id, name, calories_per_100g, proteins_per_100g,
                        carbs_per_100g, fats_per_100g, allergens_tags, cached_at)
VALUES
    ('dddd0000-0000-0000-0000-000000000001', 'dev-0001', 'Blanc de poulet', 165, 31, 0, 4, '{}', NOW()),
    ('dddd0000-0000-0000-0000-000000000002', 'dev-0002', 'Riz basmati cuit', 130, 3, 28, 0, '{}', NOW()),
    ('dddd0000-0000-0000-0000-000000000003', 'dev-0003', 'Brocoli cuit', 35, 2, 7, 0, '{}', NOW()),
    ('dddd0000-0000-0000-0000-000000000004', 'dev-0004', 'Pain complet', 247, 13, 41, 3, '{Gluten}', NOW()),
    ('dddd0000-0000-0000-0000-000000000005', 'dev-0005', 'Yaourt nature', 61, 3, 5, 3, '{Milk}', NOW())
ON CONFLICT (id) DO NOTHING;

-- ── Aliments favoris ─────────────────────────────────────────────────────────

INSERT INTO saved_food_items (id, user_id, food_item_id, saved_at)
VALUES
    ('eeee0000-0000-0000-0000-000000000001',
     'aaaa0000-0000-0000-0000-000000000001',
     'dddd0000-0000-0000-0000-000000000001', NOW()),

    ('eeee0000-0000-0000-0000-000000000002',
     'aaaa0000-0000-0000-0000-000000000001',
     'dddd0000-0000-0000-0000-000000000002', NOW())
ON CONFLICT (id) DO NOTHING;

-- ── Repas ────────────────────────────────────────────────────────────────────

INSERT INTO meals (id, user_id, name, meal_type, consumed_at, notes, is_saved, created_at)
VALUES
    ('ffff0000-0000-0000-0000-000000000001',
     'aaaa0000-0000-0000-0000-000000000001',
     'Déjeuner poulet-riz', 'Lunch', NOW() - INTERVAL '4 hours',
     'Repas type de la semaine', true, NOW())
ON CONFLICT (id) DO NOTHING;

INSERT INTO meal_items (id, meal_id, food_item_id, quantity,
                        nutrition_calories, nutrition_proteins, nutrition_carbs, nutrition_fats)
VALUES
    ('f0f0f0f0-0000-0000-0000-000000000001',
     'ffff0000-0000-0000-0000-000000000001',
     'dddd0000-0000-0000-0000-000000000001', 150, 247.5, 46, 0, 6),

    ('f0f0f0f0-0000-0000-0000-000000000002',
     'ffff0000-0000-0000-0000-000000000001',
     'dddd0000-0000-0000-0000-000000000002', 200, 260, 6, 56, 0)
ON CONFLICT (id) DO NOTHING;

COMMIT;

-- ── Vérification ─────────────────────────────────────────────────────────────
--   SELECT keycloak_id, subscription_tier FROM users;           -- 3 utilisateurs
--   SELECT COUNT(*) FROM diet_plans WHERE is_template = true;   -- 4 templates (migration)
--   SELECT COUNT(*) FROM diet_plans WHERE is_template = false;  -- 2 plans personnels
