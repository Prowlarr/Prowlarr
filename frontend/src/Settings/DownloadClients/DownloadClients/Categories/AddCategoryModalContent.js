import PropTypes from 'prop-types';
import React from 'react';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import SpinnerErrorButton from 'Components/Link/SpinnerErrorButton';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes, kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './AddCategoryModalContent.css';

function AddCategoryModalContent(props) {
  const {
    advancedSettings,
    item,
    onInputChange,
    onFieldChange,
    onCancelPress,
    onSavePress,
    onDeleteSpecificationPress,
    ...otherProps
  } = props;

  const {
    id,
    clientCategory,
    categories
  } = item;

  return (
    <ModalContent onModalClose={onCancelPress}>
      <ModalHeader>
        {id ? translate('EditCategory') : translate('AddCategory')}
      </ModalHeader>

      <ModalBody>
        <Form
          {...otherProps}
        >
          <FormGroup>
            <FormLabel>
              {translate('DownloadClientCategory')}
            </FormLabel>

            <FormInputGroup
              type={inputTypes.TEXT}
              name="clientCategory"
              {...clientCategory}
              onChange={onInputChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>
              {translate('MappedCategories')}
            </FormLabel>

            <FormInputGroup
              type={inputTypes.CATEGORY_SELECT}
              name="categories"
              {...categories}
              onChange={onInputChange}
            />
          </FormGroup>
        </Form>
      </ModalBody>
      <ModalFooter>
        {
          id &&
            <Button
              className={styles.deleteButton}
              kind={kinds.DANGER}
              onPress={onDeleteSpecificationPress}
            >
              {translate('Delete')}
            </Button>
        }

        <Button
          onPress={onCancelPress}
        >
          {translate('Cancel')}
        </Button>

        <SpinnerErrorButton
          isSpinning={false}
          onPress={onSavePress}
        >
          {translate('Save')}
        </SpinnerErrorButton>
      </ModalFooter>
    </ModalContent>
  );
}

AddCategoryModalContent.propTypes = {
  advancedSettings: PropTypes.bool.isRequired,
  item: PropTypes.object.isRequired,
  onInputChange: PropTypes.func.isRequired,
  onFieldChange: PropTypes.func.isRequired,
  onCancelPress: PropTypes.func.isRequired,
  onSavePress: PropTypes.func.isRequired,
  onDeleteSpecificationPress: PropTypes.func
};

export default AddCategoryModalContent;
