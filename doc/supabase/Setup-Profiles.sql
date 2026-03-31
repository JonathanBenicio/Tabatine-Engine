-- ==========================================
-- SETUP: User Profile Identity Linking
-- ==========================================

-- 1. Backfill profiles for existing users (prevents FK failures and ensures all users have a profile)
INSERT INTO public.perfis (id, updated_at)
SELECT id, now()
FROM auth.users
ON CONFLICT (id) DO NOTHING;

-- 2. Add Foreign Key constraint to link public.perfis.id with auth.users.id
-- This ensures that a profile cannot exist without a user and enables 
-- cascading deletes to clean up data when a user is removed.
ALTER TABLE public.perfis
ADD CONSTRAINT fk_perfil_user
FOREIGN KEY (id) REFERENCES auth.users (id)
ON DELETE CASCADE;

-- 3. Function to handle automatic profile creation on signup
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS trigger AS $$
BEGIN
  INSERT INTO public.perfis (id, updated_at)
  VALUES (new.id, now());
  RETURN new;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- 4. Trigger to call the function after a new user is inserted into auth.users
DROP TRIGGER IF EXISTS on_auth_user_created ON auth.users;
CREATE TRIGGER on_auth_user_created
  AFTER INSERT ON auth.users
  
  FOR EACH ROW EXECUTE PROCEDURE public.handle_new_user();

-- ==========================================
-- Permissions & RLS
-- ==========================================
-- Ensure RLS is enabled
ALTER TABLE public.perfis ENABLE ROW LEVEL SECURITY;

-- 1. SELECT Policy: Users can view their own profile
DROP POLICY IF EXISTS "Users can view their own profile" ON public.perfis;
CREATE POLICY "Users can view their own profile"
ON public.perfis
FOR SELECT
TO authenticated
USING ( (select auth.uid()) = id );

-- 2. UPDATE Policy: Users can update their own profile name/details
-- Condition ensures users only update their own row.
DROP POLICY IF EXISTS "Users can update their own profile" ON public.perfis;
CREATE POLICY "Users can update their own profile"
ON public.perfis
FOR UPDATE
TO authenticated
USING ( (select auth.uid()) = id )
WITH CHECK ( (select auth.uid()) = id );

-- 3. PERMISSIONS: Explicitly grant access to the 'authenticated' role
GRANT SELECT, UPDATE ON public.perfis TO authenticated;
