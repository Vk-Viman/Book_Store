describe('Admin create product flow', () => {
  it('logs in and creates a product', () => {
    cy.visit('/login');
    cy.get('input[name="email"]').type('admin@readify.local');
    cy.get('input[name="password"]').type('Readify#Admin123!');
    // wait for the submit button to be present and enabled (allow more time for Angular to render)
    cy.get('button[type="submit"]', { timeout: 10000 }).should('be.visible').should('not.be.disabled').click();

    // wait for the Admin nav link to appear and be visible (allow more time for auth/route)
    cy.contains('Admin', { timeout: 10000 }).should('be.visible').click();

    // wait for the Add product button on the admin products page
    cy.contains('Add product', { timeout: 10000 }).should('be.visible').click();

    cy.get('input[formcontrolname="title"]').type('Cypress Test Product');
    cy.get('input[formcontrolname="authors"]').type('Cypress');
    cy.get('input[formcontrolname="price"]').clear().type('5.99');
    cy.get('input[formcontrolname="stockQty"]').clear().type('7');
    cy.get('select').select(1);
    cy.get('input[formcontrolname="imageUrl"]').type('https://via.placeholder.com/150');
    cy.get('button[type="submit"]').click();

    // Back to list, expect the created product title to appear
    cy.contains('Cypress Test Product', { timeout: 10000 }).should('exist');
  });
});
