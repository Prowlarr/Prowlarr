import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Card from 'Components/Card';
import Label from 'Components/Label';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import AddCategoryModalConnector from './AddCategoryModalConnector';
import styles from './Category.css';

class Category extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isEditSpecificationModalOpen: false,
      isDeleteSpecificationModalOpen: false
    };
  }

  //
  // Listeners

  onEditSpecificationPress = () => {
    this.setState({ isEditSpecificationModalOpen: true });
  };

  onEditSpecificationModalClose = () => {
    this.setState({ isEditSpecificationModalOpen: false });
  };

  onDeleteSpecificationPress = () => {
    this.setState({
      isEditSpecificationModalOpen: false,
      isDeleteSpecificationModalOpen: true
    });
  };

  onDeleteSpecificationModalClose = () => {
    this.setState({ isDeleteSpecificationModalOpen: false });
  };

  onConfirmDeleteSpecification = () => {
    this.props.onConfirmDeleteSpecification(this.props.id);
  };

  //
  // Lifecycle

  render() {
    const {
      id,
      clientCategory,
      categories
    } = this.props;

    return (
      <Card
        className={styles.customFormat}
        overlayContent={true}
        onPress={this.onEditSpecificationPress}
      >
        <div className={styles.nameContainer}>
          <div className={styles.name}>
            {clientCategory}
          </div>
        </div>

        <Label kind={kinds.PRIMARY}>
          {`${categories.length} ${categories.length > 1 ? translate('Categories') : translate('Category')}`}
        </Label>

        <AddCategoryModalConnector
          id={id}
          isOpen={this.state.isEditSpecificationModalOpen}
          onModalClose={this.onEditSpecificationModalClose}
          onDeleteSpecificationPress={this.onDeleteSpecificationPress}
        />

        <ConfirmModal
          isOpen={this.state.isDeleteSpecificationModalOpen}
          kind={kinds.DANGER}
          title={translate('DeleteClientCategory')}
          message={
            <div>
              <div>
                {translate('AreYouSureYouWantToDeleteCategory')}
              </div>
            </div>
          }
          confirmLabel={translate('Delete')}
          onConfirm={this.onConfirmDeleteSpecification}
          onCancel={this.onDeleteSpecificationModalClose}
        />
      </Card>
    );
  }
}

Category.propTypes = {
  id: PropTypes.number.isRequired,
  categories: PropTypes.arrayOf(PropTypes.number).isRequired,
  clientCategory: PropTypes.string.isRequired,
  onConfirmDeleteSpecification: PropTypes.func.isRequired
};

export default Category;
