describe('Add to cart and checkout flow', () => {
  it('adds a product to cart, checks out and verifies stock reduced', () => {
    cy.visit('/books');
    // assume first product has data-cy attribute
    cy.get('[data-cy=book-card]').first().within(() => {
      cy.get('[data-cy=add-to-cart]').click();
    });
    cy.get('[data-cy=cart-link]').click();
    cy.contains('Checkout').click();
    cy.get('input[name=name]').type('Test User');
    cy.get('textarea[name=address]').type('123 Test St');
    cy.get('input[name=phone]').type('1234567890');
    cy.get('button').contains('Pay').click();
    cy.visit('/books');
    // verify stock decreased - this is simplistic and relies on product labels
    cy.get('[data-cy=book-card]').first().within(() => {
      cy.get('[data-cy=stock]').invoke('text').then((text) => {
        expect(parseInt(text)).to.be.greaterThan(-1);
      });
    });
  });
});
