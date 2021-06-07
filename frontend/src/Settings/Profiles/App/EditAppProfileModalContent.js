import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import SpinnerErrorButton from 'Components/Link/SpinnerErrorButton';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes, kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './EditAppProfileModalContent.css';

class EditAppProfileModalContent extends Component {

  //
  // Render

  render() {
    const {
      isFetching,
      error,
      isSaving,
      saveError,
      item,
      isInUse,
      onInputChange,
      onSavePress,
      onModalClose,
      onDeleteAppProfilePress,
      ...otherProps
    } = this.props;

    const {
      id,
      name,
      applicationIds,
      enableRss,
      enableInteractiveSearch,
      enableAutomaticSearch
    } = item;

    return (
      <ModalContent onModalClose={onModalClose}>

        <ModalHeader>
          {id ? translate('EditAppProfile') : translate('AddAppProfile')}
        </ModalHeader>

        <ModalBody>
          <div>
            {
              isFetching &&
                <LoadingIndicator />
            }

            {
              !isFetching && !!error &&
                <div>
                  {translate('UnableToAddANewAppProfilePleaseTryAgain')}
                </div>
            }

            {
              !isFetching && !error &&
                <Form
                  {...otherProps}
                >
                  <FormGroup>
                    <FormLabel>
                      {translate('Name')}
                    </FormLabel>

                    <FormInputGroup
                      type={inputTypes.TEXT}
                      name="name"
                      {...name}
                      onChange={onInputChange}
                    />
                  </FormGroup>

                  <FormGroup>
                    <FormLabel>
                      {translate('Applications')}
                    </FormLabel>

                    <FormInputGroup
                      type={inputTypes.APPLICATION_SELECT}
                      name="applicationIds"
                      {...applicationIds}
                      helpText={translate('ApplicationSelectHelpText')}
                      onChange={onInputChange}
                    />
                  </FormGroup>

                  <FormGroup>
                    <FormLabel>
                      {translate('EnableRss')}
                    </FormLabel>

                    <FormInputGroup
                      type={inputTypes.CHECK}
                      name="enableRss"
                      {...enableRss}
                      helpText={translate('EnableRssHelpText')}
                      onChange={onInputChange}
                    />
                  </FormGroup>

                  <FormGroup>
                    <FormLabel>
                      {translate('EnableInteractiveSearch')}
                    </FormLabel>

                    <FormInputGroup
                      type={inputTypes.CHECK}
                      name="enableInteractiveSearch"
                      {...enableInteractiveSearch}
                      helpText={translate('EnableInteractiveSearchHelpText')}
                      onChange={onInputChange}
                    />
                  </FormGroup>

                  <FormGroup>
                    <FormLabel>
                      {translate('EnableAutomaticSearch')}
                    </FormLabel>

                    <FormInputGroup
                      type={inputTypes.CHECK}
                      name="enableAutomaticSearch"
                      {...enableAutomaticSearch}
                      helpText={translate('EnableAutomaticSearchHelpText')}
                      onChange={onInputChange}
                    />
                  </FormGroup>
                </Form>
            }
          </div>
        </ModalBody>
        <ModalFooter>
          {
            id ?
              <div
                className={styles.deleteButtonContainer}
                title={
                  isInUse ?
                    translate('AppProfileInUse') :
                    undefined
                }
              >
                <Button
                  kind={kinds.DANGER}
                  isDisabled={isInUse}
                  onPress={onDeleteAppProfilePress}
                >
                  {translate('Delete')}
                </Button>
              </div> :
              null
          }

          <Button
            onPress={onModalClose}
          >
            {translate('Cancel')}
          </Button>

          <SpinnerErrorButton
            isSpinning={isSaving}
            error={saveError}
            onPress={onSavePress}
          >
            {translate('Save')}
          </SpinnerErrorButton>
        </ModalFooter>
      </ModalContent>
    );
  }
}

EditAppProfileModalContent.propTypes = {
  isFetching: PropTypes.bool.isRequired,
  error: PropTypes.object,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  item: PropTypes.object.isRequired,
  isInUse: PropTypes.bool.isRequired,
  onInputChange: PropTypes.func.isRequired,
  onSavePress: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired,
  onDeleteAppProfilePress: PropTypes.func
};

export default EditAppProfileModalContent;
