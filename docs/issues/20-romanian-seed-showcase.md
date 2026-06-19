# Slice 19 — Romanian seed + "Clasa Steluțelor" showcase

**Type:** AFK

## Parent

#1 — PRD: Classroom Manager

## What to build

Localize the default behavior catalog to Romanian and rebuild the demo showcase as "Clasa Steluțelor", so every redesigned screen demos against rich, Romanian, realistic data.

New classes seed the six Romanian default behaviors (signed points):

- −3 — s-a bătut cu un coleg
- −2 — nu și-a făcut tema
- −1 — a deranjat ora
- +1 — a răspuns bine
- +2 — a ajutat un coleg
- +3 — a rezolvat o problemă dificilă

The demo seeder builds a class named **Clasa Steluțelor** with the star currency icon, the six Romanian behaviors, and the 24-student roster copied from the original prototype (name + gender):

Băncilă Amira (F), Beju Matei Tudor (M), Bogdan Nicole Alexandra (F), Căpraru Mihai (M), Ciucă Eva Maria (F), Dancu Desyre (F), David Eric (M), Falcusan Nicholas (M), Ghițescu Dragoș Valentin (M), Goga Sara Ioana (F), Ienchi Deian Ioan (M), Izvernari Vanessa Cataleea (F), Mârza Sofia (F), Moldoveanu Rareș Andrei (M), Motre Alexandru (M), Ostafe Sara Maria (F), Parasca David Andrei (M), Prichici Ana Carolina (F), Ranjous Rayan (M), Sperdea Amelia Andreea (F), Struna Vladimir (M), Serban Selena (F), Todor Maria Antonia (F), Văran Caius Andrei (M).

On top of the roster it builds a rich, mostly-positive multi-week point history, some avatar purchases for higher earners (never overspending the running wallet), and at least one saved grouping — all deterministic from a fixed seed, exactly as the existing demo seeder does.

## Acceptance criteria

- [ ] New classes receive the six Romanian default behaviors with correct signed points.
- [ ] The demo account's class is "Clasa Steluțelor" with the 24-student Romanian roster and the star currency icon.
- [ ] The demo class has mostly-positive multi-week history, some non-overspending purchases, and at least one saved grouping.
- [ ] The re-seed wipe stays scoped strictly to the demo teacher's own class (never touches the real owner).
- [ ] Existing demo/seed tests pass; "Try the demo" lands in the populated "Clasa Steluțelor".

## Blocked by

- #19 — Per-class currency icon

## Commit on completion

After completing this issue, create a new git commit with a descriptive message summarizing what was achieved (the issue title is acceptable).
